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

# DÙNG MODEL MỚI NHẤT
MODEL_NAME = "gemini-2.5-pro"

try:
    model = genai.GenerativeModel(MODEL_NAME)
    print(f"Đã kết nối thành công với model: {MODEL_NAME}")
except Exception as e:
    print(f"Lỗi khi khởi tạo model: {e}")
    exit(1)

app = Flask(__name__)


@app.route('/analyze', methods=['POST'])
def analyze_food():
    if 'image' not in request.files: 
        return jsonify({"error": "Không có ảnh"}), 400 
    file = request.files['image'] 

    # Lấy TDEE và Goal từ C#
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
        img_bytes = file.read() 
        img = Image.open(io.BytesIO(img_bytes)) 

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
        response = model.generate_content([prompt, img]) 
        raw_text = response.text.strip() 

        json_match = re.search(r'\{.*\}', raw_text, re.DOTALL) 
        if not json_match: 
            return jsonify({
                "error": "AI không trả JSON", 
                "raw_response": raw_text[:500] 
            }), 500 
        json_str = json_match.group(0) 
        data = json.loads(json_str) 
        
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
        chat_context = data.get('chat_context', []) 

        if not user_question:
            return jsonify({"error": "Vui lòng nhập câu hỏi."}), 400

        chat_history_formatted = "\n".join([
            f"{c['Role']}: {c['Content']}" for c in chat_context
        ])

        context_prompt = f"""
        Bạn là chuyên gia dinh dưỡng cá nhân có tên là NutritionAI.
        Nhiệm vụ của bạn là đưa ra lời khuyên dựa trên Dữ liệu người dùng:
        - Tuổi: {user_stats.get('Age')}
        - Cân nặng: {user_stats.get('Weight')}kg
        - Mục tiêu: {user_stats.get('Goal')}
        - TDEE: {user_stats.get('TDEE')} kcal/ngày

        --- LỊCH SỬ ĂN UỐNG GẦN ĐÂY (5 bữa):
        {json.dumps(food_history, ensure_ascii=False, indent=2)}
        
        --- LỊCH SỬ ĐỐI THOẠI TRƯỚC:
        {chat_history_formatted}

        Hãy trả lời câu hỏi của người dùng một cách thân thiện, chi tiết và cá nhân hóa. 
        Sử dụng định dạng **in đậm** cho các điểm quan trọng. 
        Nếu lịch sử ăn uống cho thấy sự mất cân bằng so với mục tiêu, hãy cảnh báo 
        và gợi ý món ăn thay thế/bổ sung.
        Câu hỏi của người dùng: {user_question}
        """

        chat_model = genai.GenerativeModel("gemini-2.5-flash")
        response = chat_model.generate_content(context_prompt)

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
        
        goal_text = f"Mục tiêu của bạn ({user_stats['Goal']}) là: Protein {macro_goals.get('ProteinGrams', 0)}g, Carbs {macro_goals.get('CarbGrams', 0)}g, Fat {macro_goals.get('FatGrams', 0)}g (TDEE {user_stats['TDEE']} kcal)."
        
        summary_text = f"Hôm nay bạn đã nạp: {daily_summary['TotalCalories']} kcal. Macros đã nạp: Protein {daily_summary['TotalProtein']}g, Carbs {daily_summary['TotalCarbs']}g, Fat {daily_summary['TotalFat']}g."
        
        history_formatted = "\n".join([
            f"- {f['MealType']}: {f['FoodName']} ({f['Calories']} kcal, P{f['Protein']} C{f['Carbs']} F{f['Fat']})" for f in today_history
        ])

        proactive_prompt = f"""
        Bạn là NutritionAI, chuyên gia dinh dưỡng chủ động. Nhiệm vụ của bạn là phân tích dữ liệu ăn uống hôm nay của người dùng so với mục tiêu Macro và TDEE của họ, sau đó đưa ra lời khuyên hoặc đề xuất bữa ăn tiếp theo.

        --- DỮ LIỆU MỤC TIÊU ---
        {goal_text}

        --- TỔNG KẾT ĐÃ NẠP HÔM NAY ---
        {summary_text}
        
        --- LỊCH SỬ CHI TIẾT HÔM NAY ---
        {history_formatted if history_formatted else "Chưa có dữ liệu ăn uống hôm nay."}

        YÊU CẦU:
        1. Phân tích xem người dùng đã thiếu hụt hay vượt quá Calo/Macros nào so với Mục tiêu hàng ngày.
        2. Đưa ra **LỜI KHUYÊN CHỦ ĐỘNG** ngắn gọn (dưới 5 câu) cho bữa ăn tiếp theo.
        3. Sử dụng định dạng **in đậm** cho các cảnh báo quan trọng.
        4. KHÔNG trả về JSON. Chỉ trả lời bằng văn bản.
        """

        chat_model = genai.GenerativeModel(MODEL_NAME)
        response = chat_model.generate_content(proactive_prompt)
        
        return jsonify({
            "advice": response.text.strip()
        })
        
    except Exception as e:
        return jsonify({"error": f"Lỗi xử lý AI Proactive: {str(e)}"}), 500
    

@app.route('/generate_recipe', methods=['POST'])
def generate_recipe():
    try:
        data = request.get_json()
        goal = data['goal']
        tdee = data['TDEE']
        macros = data['macro_goals']
        custom_request = data['custom_request']

        try:
            target_calories = float(tdee) * 0.3
        except ValueError:
            target_calories = 600

        prompt = f"""
        Bạn là chuyên gia ẩm thực và dinh dưỡng.
        Yêu cầu: Tạo một công thức món ăn hoàn chỉnh phù hợp với yêu cầu: '{custom_request}'.

        Dữ liệu Mục tiêu người dùng:
        - Mục tiêu: {goal}
        - TDEE: {tdee} kcal
        - Mục tiêu Macros HÀNG NGÀY: Protein {macros.get('ProteinGrams', 0):.0f}g, Carbs {macros.get('CarbGrams', 0):.0f}g, Fat {macros.get('FatGrams', 0):.0f}g.

        RÀNG BUỘC CALO: Món ăn này phải có tổng Calo KHÔNG VƯỢT QUÁ {target_calories:.0f} kcal và cân bằng Macros phù hợp với mục tiêu '{goal}'.

        Trả về đúng định dạng JSON sau, KHÔNG thêm bất kỳ văn bản nào bên ngoài:

        {{
            "Title": "Tên món ăn bằng tiếng Việt",
            "Description": "Mô tả ngắn gọn về món ăn.",
            "Ingredients": ["Nguyên liệu 1", "Nguyên liệu 2", ...],
            "Instructions": ["Bước 1", "Bước 2", ...],
            "CaloriesTotal": số nguyên,
            "ProteinGrams": số thực,
            "CarbGrams": số thực,
            "FatGrams": số thực,
            "Advice": "Lời khuyên ngắn gọn về món ăn này so với mục tiêu Macro/Calo."
        }}
        BẮT BUỘC: Chỉ trả JSON.
        """

        response = model.generate_content([prompt])
        raw_text = response.text.strip()

        json_match = re.search(r'\{.*\}', raw_text, re.DOTALL)
        if not json_match:
            return jsonify({
                "error": "AI không trả JSON hợp lệ cho Recipe",
                "raw_response": raw_text[:500]
            }), 500
            
        json_str = json_match.group(0)
        data = json.loads(json_str)

        return jsonify(data)

    except Exception as e:
        return jsonify({"error": f"Lỗi xử lý Recipe: {str(e)}"}), 500


if __name__ == '__main__':
    print("Flask AI Service đang chạy tại: http://localhost:5000")
    print("Dùng model:", MODEL_NAME)
    app.run(host='0.0.0.0', port=5000, debug=True)