from flask import Flask, request, jsonify
from PIL import Image
import io
import google.generativeai as genai
from dotenv import load_dotenv
import os
import json
import re

# Load API Key
load_dotenv()
API_KEY = os.getenv("GEMINI_API_KEY")
if not API_KEY:
    raise ValueError("GEMINI_API_KEY không tồn tại trong .env")

genai.configure(api_key=API_KEY)

# DÙNG MODEL MỚI NHẤT TỪ DANH SÁCH CỦA BẠN
MODEL_NAME = "gemini-2.5-flash"  # Ổn định, nhanh, hỗ trợ ảnh

try:
    model = genai.GenerativeModel(MODEL_NAME)
    print(f"Đã kết nối thành công với model: {MODEL_NAME}")
except Exception as e:
    print(f"Lỗi khi khởi tạo model: {e}")
    exit(1)

app = Flask(__name__)

# Hàm Python sẽ mô phỏng việc gọi API của ASP.NET Core
def get_exercise_videos(muscle_group: str):
    """
    Tra cứu danh sách video bài tập (YoutubeVideoUrl) dựa trên nhóm cơ
    cụ thể (ví dụ: 'Bụng', 'Ngực', 'Đùi').
    """
    import requests
    import json
    # Gọi endpoint API ASP.NET Core vừa tạo
    backend_url = f"http://localhost:5196/Exercise/GetVideosJson?group={muscle_group}"
    
    try:
        response = requests.get(backend_url)
        if response.status_code == 200:
            return json.loads(response.text)
        else:
            return {"error": "Lỗi truy vấn backend"}
    except requests.exceptions.RequestException as e:
        return {"error": f"Lỗi kết nối ASP.NET Core: {e}"}

# Đăng ký các hàm (tools) để Gemini có thể sử dụng
tools = [get_exercise_videos]

@app.route('/analyze', methods=['POST'])
def analyze_food():
    if 'image' not in request.files: 
        return jsonify({"error": "Không có ảnh"}), 400 
    file = request.files['image'] 

    # Lấy TDEE từ C# gửi sang
    tdee_input = request.form.get('tdee', '0')
    goal_input = request.form.get('goal', 'Chưa thiết lập') 

    tdee = 0
    if tdee_input and tdee_input != '0':
        try:
            tdee = float(tdee_input)
        except ValueError:
            pass 

    if file.filename == '': 
        return jsonify({"error": "Tên file rỗng"}), 400 
    try:
        # Đọc ảnh 
        img_bytes = file.read() 
        img = Image.open(io.BytesIO(img_bytes)) 

        # CẬP NHẬT PROMPT ĐỂ CÁ NHÂN HÓA 
        goal_description = f"Mục tiêu dinh dưỡng của người dùng là: {goal_input}."
        
        if tdee > 0:
            tdee_str = f"TDEE của người dùng là {tdee:.0f} kcal/ngày. {goal_description} Dựa vào TDEE và Mục tiêu này để đưa ra cảnh báo burn_time và advice cá nhân hóa (ví dụ: món này chiếm X% TDEE và KHÔNG phù hợp với mục tiêu {goal_input})."
        else:
            tdee_str = f"Không có thông tin TDEE người dùng, chỉ có Mục tiêu: {goal_input}."
            
        prompt = f"""
        Bạn là chuyên gia dinh dưỡng. Phân tích ảnh món ăn và trả về đúng định dạng JSON sau, KHÔNG thêm bất kỳ giải thích nào:
        {tdee_str}
        YÊU CẦU: Nếu món ăn có warning=true (calo cao/đồ ăn vặt), hãy tính burn_time VÀ mô tả cảnh báo dựa trên TDEE của người dùng (Ví dụ: Món này chiếm 30% TDEE hàng ngày của bạn).
        {{
        "food_name": "Tên món ăn bằng tiếng Việt (ví dụ: Phở bò, Trà sữa trân châu)", 
        "calories": số nguyên (calo cho 1 phần ăn trong ảnh), 
        "protein": số thực (gram), 
        "carbs": số thực (gram),
        "fat": số thực (gram), 
        "warning": true hoặc false (true nếu là đồ ăn vặt, trà sữa, chiên rán, nhiều đường),         
        "burn_time": "Cần chạy bộ X phút để tiêu hao" (chỉ nếu warning=true, X = calories / 10), 
        "advice": "Lời khuyên cá nhân hóa ngắn gọn dựa trên TDEE và mục tiêu người dùng" (chỉ nếu warning=true)
        }}
        BẮT BUỘC: Chỉ trả JSON, không xuống dòng, không giải thích. 
        """
        # Gọi Gemini 
        response = model.generate_content([prompt, img]) 
        raw_text = response.text.strip() 

        # Làm sạch JSON 
        json_match = re.search(r'\{.*\}', raw_text, re.DOTALL) 
        if not json_match: 
            return jsonify({
                "error": "AI không trả JSON", 
                "raw_response": raw_text[:500] 
            }), 500 
        json_str = json_match.group(0) 
        data = json.loads(json_str) 
        
        # Dự phòng burn_time 
        if data.get("warning") and "calories" in data and not data.get("burn_time"): 
            minutes = int(data["calories"] / 10) 
            data["burn_time"] = f"Cần chạy bộ {minutes} phút để tiêu hao" 
            
        return jsonify(data) 
    except Exception as e:
        return jsonify({"error": f"Lỗi xử lý: {str(e)}"}), 500 
    
@app.route('/advise_chat', methods=['POST'])
def advise_chat():
    try:
        data = request.get_json()
        user_question = data.get('question')
        user_stats = data.get('stats')
        food_history = data.get('history')
        # NHẬN DỮ LIỆU CHAT HISTORY MỚI
        chat_context = data.get('chat_context', []) 

        if not user_question:
            return jsonify({"error": "Vui lòng nhập câu hỏi."}), 400

        # FORMAT LỊCH SỬ CHAT VÀO TEXT
        chat_history_formatted = "\n".join([
            f"{c['Role']}: {c['Content']}" for c in chat_context
        ])

        # 1. Xây dựng Ngữ cảnh (System Prompt)
        context_prompt = f"""
        Bạn là chuyên gia dinh dưỡng cá nhân có tên là NutritionAI.
        Nhiệm vụ của bạn là đưa ra lời khuyên dựa trên Dữ liệu người dùng:
        - Tuổi: {user_stats.get('Age')}
        - Cân nặng: {user_stats.get('Weight')}kg
        - Mục tiêu: {user_stats.get('Goal')}
        - TDEE: {user_stats.get('TDEE')} kcal/ngày

        --- LỊCH SỬ ĂN UỐNG GẦN ĐÂY (5 bữa):
        {food_history}
        
        --- LỊCH SỬ ĐỐI THOẠI TRƯỚC (Giúp bạn nhớ ngữ cảnh):
        {chat_history_formatted}

        Hãy trả lời câu hỏi của người dùng một cách thân thiện, chi tiết và cá nhân hóa. 
        Sử dụng định dạng **in đậm** cho các điểm quan trọng. 
        Nếu lịch sử ăn uống cho thấy sự mất cân bằng so với mục tiêu, hãy cảnh báo 
        và gợi ý món ăn thay thế/bổ sung.
        """
        # Thêm câu hỏi người dùng vào context
        full_prompt = f"{context_prompt}\nCâu hỏi của người dùng: {user_question}"

        # 2. Xây dựng nội dung cho Gemini
        # Vì Gemini có thể duy trì hội thoại bằng cách gửi tất cả lịch sử, 
        # chúng ta sẽ gửi prompt ngữ cảnh và câu hỏi cuối cùng
        
        # contents = [
        #     {"role": "user", "parts": [{"text": context_prompt}]},
        #     {"role": "user", "parts": [{"text": user_question}]}
        # ]
        
        # 3. Gọi Gemini Chat API
        chat_model = genai.GenerativeModel("gemini-2.5-flash") # [6, 7]
        # response = chat_model.generate_content(contents,tools=tools)
        response = chat_model.generate_content( full_prompt,tools=tools)
        # --- BƯỚC XỬ LÝ FUNCTION CALL (MỚI) ---
        if response.function_calls:
            function_call = response.function_calls
            function_name = function_call.name
            args = dict(function_call.args)
            # Chỉ hỗ trợ hàm get_exercise_videos
            if function_name == 'get_exercise_videos':
                # Thực thi hàm Python mô phỏng truy vấn database
                function_response = get_exercise_videos(**args)

                # Gửi kết quả trở lại cho Gemini để tạo câu trả lời
                response = chat_model.generate_content(
                    [
                        full_prompt,
                        response.candidates.content,
                        {"role": "function", 
                         "name": function_name,
                         "parts": [{"text": json.dumps(function_response)}]}
                    ],
                    tools=tools # Tiếp tục cho phép gọi tools
                )
                # Trả về kết quả cuối cùng từ AI
                return jsonify({"advice": response.text.strip()})
        
        return jsonify({
            "advice": response.text.strip()
        })
    except Exception as e:
        return jsonify({"error": f"Lỗi xử lý AI Chat: {str(e)}"}), 500
    
@app.route('/proactive_advise', methods=['POST'])
def proactive_advise():
    try:
        data = request.get_json()
        user_stats = data.get('stats', {})
        macro_goals = data.get('macroGoals', {})
        daily_summary = data.get('dailySummary', {})
        today_history = data.get('history', [])

        if not user_stats.get('TDEE') or not macro_goals:
            return jsonify({"advice": "**Lỗi:** Không có dữ liệu cá nhân (TDEE, Mục tiêu Macro). Vui lòng kiểm tra lại Cài đặt."})
        
        # Format Mục tiêu Macro
        goal_text = f"Mục tiêu của bạn ({user_stats['Goal']}) là: Protein {macro_goals.get('ProteinGrams', 0)}g, Carbs {macro_goals.get('CarbGrams', 0)}g, Fat {macro_goals.get('FatGrams', 0)}g (TDEE {user_stats['TDEE']} kcal)."
        
        # Format Tổng hợp đã ăn hôm nay
        summary_text = f"Hôm nay bạn đã nạp: {daily_summary['TotalCalories']} kcal. Macros đã nạp: Protein {daily_summary['TotalProtein']}g, Carbs {daily_summary['TotalCarbs']}g, Fat {daily_summary['TotalFat']}g."
        
        # Format Lịch sử chi tiết
        history_formatted = "\n".join([
            f"- {f['MealType']}: {f['FoodName']} ({f['Calories']} kcal, P{f['Protein']} C{f['Carbs']} F{f['Fat']})" for f in today_history
        ])

        # 1. Xây dựng Ngữ cảnh Chủ động (System Prompt)
        proactive_prompt = f"""
        Bạn là NutritionAI, chuyên gia dinh dưỡng chủ động. Nhiệm vụ của bạn là phân tích dữ liệu ăn uống hôm nay của người dùng so với mục tiêu Macro và TDEE của họ, sau đó đưa ra lời khuyên hoặc đề xuất bữa ăn tiếp theo (dựa trên bữa ăn cuối cùng).

        --- DỮ LIỆU MỤC TIÊU ---
        {goal_text}

        --- TỔNG KẾT ĐÃ NẠP HÔM NAY ---
        {summary_text}
        
        --- LỊCH SỬ CHI TIẾT HÔM NAY ---
        {history_formatted if history_formatted else "Chưa có dữ liệu ăn uống hôm nay."}

        YÊU CẦU:
        1. Phân tích xem người dùng đã thiếu hụt hay vượt quá Calo/Macros nào so với Mục tiêu hàng ngày (Macro Goals).
        2. Dựa trên thời điểm hiện tại và dữ liệu đã nạp, hãy đưa ra **LỜI KHUYÊN CHỦ ĐỘNG** ngắn gọn (dưới 5 câu) cho bữa ăn tiếp theo hoặc hoạt động cần thiết.
        3. Sử dụng định dạng **in đậm** cho các cảnh báo quan trọng.
        4. KHÔNG trả lời dưới dạng JSON. Chỉ trả về lời khuyên hữu ích dưới dạng văn bản.
        """

        # 2. Gọi Gemini Content API
        
        # Chúng ta chỉ cần gửi proactive_prompt một lần để Gemini phân tích
        contents = [{"role": "user", "parts": [{"text": proactive_prompt}]}]
        
        chat_model = genai.GenerativeModel(MODEL_NAME)
        response = chat_model.generate_content(contents)
        
        return jsonify({
            "advice": response.text.strip()
        })
        
    except Exception as e:
        return jsonify({"error": f"Lỗi xử lý AI Proactive: {str(e)}"}), 500

@app.route('/exercise_advise', methods=['POST'])
def exercise_advise():
    try:
        data = request.get_json()
        user_stats = data.get('stats', {})
        daily_summary = data.get('dailySummary', {})
        available_groups = data.get('availableGroups', [])
        
        tdee = float(user_stats.get('TDEE', 0))
        total_calories = float(daily_summary.get('TotalCalories', 0))
        
        calo_diff = tdee - total_calories
        
        # Xây dựng prompt dựa trên tình trạng Calo
        if calo_diff > 100:
            status_text = f"Bạn đã nạp {total_calories:.0f} kcal hôm nay. Bạn đang THIẾU {calo_diff:.0f} kcal so với mục tiêu TDEE. Hãy gợi ý bài tập TĂNG CƠ, TĂNG SỨC BỀN."
        elif calo_diff < -100:
            # Ước tính thời gian chạy bộ cần thiết: 10 kcal/phút
            excess_calories = abs(calo_diff)
            run_time = excess_calories / 10
            status_text = f"Bạn đã nạp {total_calories:.0f} kcal hôm nay, vượt quá mục tiêu TDEE ({tdee:.0f} kcal) khoảng {excess_calories:.0f} kcal. Gợi ý bài tập ĐỐT CHÁY MỠ và Calo DƯ THỪA. Ưu tiên bài tập có thể đốt cháy lượng calo dư thừa này ({run_time:.0f} phút chạy bộ)."
        else:
            status_text = f"Lượng calo đã nạp hôm nay ({total_calories:.0f} kcal) đang RẤT CÂN BẰNG với TDEE ({tdee:.0f} kcal). Gợi ý bài tập duy trì hoặc tăng cường thể lực nhẹ nhàng."
            
        
        exercise_prompt = f"""
        Bạn là chuyên gia thể hình và dinh dưỡng (NutritionAI).
        Dữ liệu hiện tại của người dùng:
        - Mục tiêu: {user_stats.get('Goal')}
        - {status_text}
        - Các nhóm cơ có sẵn trong thư viện: {', '.join(available_groups)}
        
        YÊU CẦU:
        1. Phân tích tình trạng calo trên.
        2. Dựa trên Mục tiêu ({user_stats.get('Goal')}) và tình trạng calo, hãy đưa ra một **LỜI KHUYÊN BÀI TẬP CÁ NHÂN HÓA** ngắn gọn.
        3. Đề xuất một nhóm cơ cụ thể (ví dụ: 'Bụng' hoặc 'Lưng') trong danh sách có sẵn mà người dùng nên tập hôm nay để đạt mục tiêu.
        4. Trả về đúng định dạng JSON, KHÔNG có lời giải thích nào khác.
        {{
            "advice_summary": "Tóm tắt lời khuyên (dưới 3 câu).",
            "suggestion_type": "Đốt mỡ / Tăng cơ / Duy trì",
            "muscle_group_focus": "Tên nhóm cơ đề xuất (VD: Bụng)",
            "required_burn_time": "{run_time:.0f} phút" 
            # Chỉ hiển thị nếu vượt calo, nếu không thì ghi 'Không áp dụng'
        }}
        """
        
        response = model.generate_content([exercise_prompt])
        raw_text = response.text.strip()
        
        # Làm sạch JSON (Tương tự như /analyze)
        json_match = re.search(r'\{.*\}', raw_text, re.DOTALL)
        if not json_match:
            return jsonify({"error": "AI không trả JSON hợp lệ", "raw": raw_text}), 500
        
        data = json.loads(json_match.group(0))
        return jsonify(data)

    except Exception as e:
        return jsonify({"error": f"Lỗi xử lý AI Exercise: {str(e)}"}), 500

if __name__ == '__main__':
    print("Flask AI Service đang chạy tại: http://localhost:5000")
    print("Dùng model:", MODEL_NAME)
    app.run(host='0.0.0.0', port=5000, debug=True)