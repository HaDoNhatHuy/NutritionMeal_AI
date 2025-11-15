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
MODEL_NAME = "gemini-2.5-flash"

try:
    model = genai.GenerativeModel(MODEL_NAME)
    print(f"Đã kết nối với model: {MODEL_NAME}")
except Exception as e:
    print(f"Lỗi model: {e}")
    exit(1)

app = Flask(__name__)

@app.route('/analyze', methods=['POST'])
def analyze_food():
    if 'image' not in request.files:
        return jsonify({"error": "Không có ảnh"}), 400
    
    file = request.files['image']
    tdee_input = request.form.get('tdee', '0')
    goal_input = request.form.get('goal', 'Chưa thiết lập')
    pathology_input = request.form.get('pathology', 'Không có')
    
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
        
        goal_description = f"Mục tiêu: {goal_input}."
        
        if tdee > 0:
            tdee_str = f"TDEE: {tdee:.0f} kcal/ngày. {goal_description} Đánh giá món ăn dựa trên TDEE và mục tiêu."
        else:
            tdee_str = f"Không có TDEE. Mục tiêu: {goal_description}."
        
        prompt = f"""
        Bạn là chuyên gia dinh dưỡng. Phân tích món ăn và trả về JSON:
        {tdee_str}
        
        Tình trạng bệnh lý: {pathology_input}
        
        YÊU CẦU: Nếu món ăn có calo cao/không lành mạnh HOẶC không phù hợp với bệnh lý, đặt warning=true và tính burn_time.
        
        {{
            "food_name": "Tên món ăn tiếng Việt",
            "calories": số nguyên,
            "protein": số thực (gram),
            "carbs": số thực (gram),
            "fat": số thực (gram),
            "warning": true/false,
            "burn_time": "Cần chạy X phút" (nếu warning=true, X=calories/10),
            "advice": "Lời khuyên ngắn gọn" (nếu warning=true)
        }}
        
        CHỈ TRẢ JSON, KHÔNG GIẢI THÍCH.
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
            data["burn_time"] = f"Cần chạy bộ {minutes} phút"
        
        return jsonify(data)
    
    except Exception as e:
        return jsonify({"error": f"Lỗi: {str(e)}"}), 500

@app.route('/advise_chat', methods=['POST'])
def advise_chat():
    try:
        data = request.get_json()
        user_question = data.get('question')
        user_stats = data.get('stats')
        food_history = data.get('history')
        chat_context = data.get('chat_context', [])
        pathology = user_stats.get('Pathology', 'Không có')
        
        if not user_question:
            return jsonify({"error": "Vui lòng nhập câu hỏi."}), 400
        
        chat_history_formatted = "\n".join([
            f"{c['Role']}: {c['Content']}" for c in chat_context
        ])
        
        context_prompt = f"""
        Bạn là NutritionAI - chuyên gia dinh dưỡng cá nhân.
        
        Dữ liệu người dùng:
        - Tuổi: {user_stats.get('Age')}
        - Cân nặng: {user_stats.get('Weight')}kg
        - Mục tiêu: {user_stats.get('Goal')}
        - TDEE: {user_stats.get('TDEE')} kcal/ngày
        - Bệnh lý: {pathology}
        
        QUAN TRỌNG: Nếu có bệnh lý, mọi lời khuyên phải tuân thủ nghiêm ngặt các kiêng kỵ.
        
        Lịch sử ăn uống (5 bữa gần đây):
        {json.dumps(food_history, ensure_ascii=False, indent=2)}
        
        Lịch sử đối thoại:
        {chat_history_formatted}
        
        Trả lời câu hỏi thân thiện, chi tiết, cá nhân hóa.
        Sử dụng **in đậm** cho điểm quan trọng.
        
        Câu hỏi: {user_question}
        """
        
        chat_model = genai.GenerativeModel(MODEL_NAME)
        response = chat_model.generate_content(context_prompt)
        
        return jsonify({"advice": response.text.strip()})
    
    except Exception as e:
        return jsonify({"error": f"Lỗi AI Chat: {str(e)}"}), 500

@app.route('/proactive_advise', methods=['POST'])
def proactive_advise():
    try:
        data = request.get_json()
        user_stats = data.get('stats', {})
        macro_goals = data.get('macroGoals', {})
        daily_summary = data.get('dailySummary', {})
        today_history = data.get('history', [])
        
        if not user_stats.get('TDEE') or not macro_goals:
            return jsonify({"advice": "**Lỗi:** Không có dữ liệu TDEE/Macro. Vui lòng kiểm tra Cài đặt."})
        
        goal_text = f"Mục tiêu ({user_stats['Goal']}): Protein {macro_goals.get('ProteinGrams', 0)}g, Carbs {macro_goals.get('CarbGrams', 0)}g, Fat {macro_goals.get('FatGrams', 0)}g (TDEE {user_stats['TDEE']} kcal)."
        
        summary_text = f"Hôm nay đã nạp: {daily_summary['TotalCalories']} kcal. Macros: P{daily_summary['TotalProtein']}g C{daily_summary['TotalCarbs']}g F{daily_summary['TotalFat']}g."
        
        history_formatted = "\n".join([
            f"- {f['MealType']}: {f['FoodName']} ({f['Calories']} kcal, P{f['Protein']} C{f['Carbs']} F{f['Fat']})" for f in today_history
        ])
        
        proactive_prompt = f"""
        Bạn là NutritionAI. Phân tích dữ liệu ăn uống hôm nay so với mục tiêu Macro/TDEE, sau đó đưa ra lời khuyên hoặc đề xuất bữa ăn tiếp theo.
        
        Mục tiêu:
        {goal_text}
        
        Tổng kết hôm nay:
        {summary_text}
        
        Chi tiết:
        {history_formatted if history_formatted else "Chưa có dữ liệu."}
        
        YÊU CẦU:
        1. Phân tích thiếu hụt/vượt quá Calo/Macros.
        2. Đưa lời khuyên chủ động ngắn gọn (<5 câu) cho bữa ăn tiếp theo.
        3. Dùng **in đậm** cho cảnh báo quan trọng.
        4. CHỈ TRẢ VĂN BẢN, KHÔNG JSON.
        """
        
        chat_model = genai.GenerativeModel(MODEL_NAME)
        response = chat_model.generate_content(proactive_prompt)
        
        return jsonify({"advice": response.text.strip()})
    
    except Exception as e:
        return jsonify({"error": f"Lỗi Proactive: {str(e)}"}), 500

@app.route('/generate_recipe', methods=['POST'])
def generate_recipe():
    try:
        data = request.get_json()
        goal = data['goal']
        tdee = data['TDEE']
        macros = data['macro_goals']
        custom_request = data['custom_request']
        pathology = data.get('Pathology', 'Không có')
        
        try:
            target_calories = float(tdee) * 0.3
        except ValueError:
            target_calories = 600
        
        prompt = f"""
        Bạn là chuyên gia ẩm thực và dinh dưỡng.
        Tạo công thức món ăn hoàn chỉnh phù hợp với: '{custom_request}'.
        
        Mục tiêu người dùng:
        - Mục tiêu: {goal}
        - TDEE: {tdee} kcal
        - Bệnh lý: {pathology}
        - Macro hàng ngày: Protein {macros.get('ProteinGrams', 0):.0f}g, Carbs {macros.get('CarbGrams', 0):.0f}g, Fat {macros.get('FatGrams', 0):.0f}g
        
        QUAN TRỌNG: Món ăn phải an toàn với bệnh lý: **{pathology}**.
        Calo món ăn KHÔNG QUÁ {target_calories:.0f} kcal.
        
        Trả về JSON:
        {{
            "Title": "Tên món",
            "Description": "Mô tả ngắn",
            "Ingredients": ["Nguyên liệu 1", ...],
            "Instructions": ["Bước 1", ...],
            "CaloriesTotal": số nguyên,
            "ProteinGrams": số thực,
            "CarbGrams": số thực,
            "FatGrams": số thực,
            "Advice": "Lời khuyên về món ăn"
        }}
        
        CHỈ TRẢ JSON.
        """
        
        response = model.generate_content([prompt])
        raw_text = response.text.strip()
        
        json_match = re.search(r'\{.*\}', raw_text, re.DOTALL)
        if not json_match:
            return jsonify({
                "error": "AI không trả JSON hợp lệ",
                "raw_response": raw_text[:500]
            }), 500
        
        json_str = json_match.group(0)
        data = json.loads(json_str)
        
        return jsonify(data)
    
    except Exception as e:
        return jsonify({"error": f"Lỗi Recipe: {str(e)}"}), 500

if __name__ == '__main__':
    print("Flask AI Service: http://localhost:5000")
    print("Model:", MODEL_NAME)
    app.run(host='0.0.0.0', port=5000, debug=True)