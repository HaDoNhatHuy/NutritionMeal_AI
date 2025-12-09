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
    deep_analysis_flag = request.form.get('deepAnalysis', 'false') == 'true' # <-- F13: ĐỌC FLAG

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

        # CẬP NHẬT PROMPT ĐỂ HỖ TRỢ PHÂN TÍCH SÂU (F13)
        if deep_analysis_flag:
            json_structure = f"""
            {{
            "food_name": "Tên món ăn tiếng Việt",
            "calories": số nguyên,
            "protein": số thực (gram),
            "carbs": số thực (gram),
            "fat": số thực (gram),
            "warning": true/false,
            "burn_time": "Cần chạy X phút" (nếu warning=true, X=calories/10),
            "advice": "Lời khuyên ngắn gọn",
            "detailed_components": [
                {{ "name": "Thành phần 1", "calories": 100, "protein": 5.0 }},
                {{ "name": "Thành phần 2", "calories": 200, "protein": 10.0 }}
            ]
            }}
            """
            prompt = f"""
            Bạn là chuyên gia dinh dưỡng. Phân tích món ăn chi tiết trong ảnh và trả về JSON.
            {tdee_str}
            Tình trạng bệnh lý: {pathology_input}
            YÊU CẦU: Thêm mảng JSON chi tiết 'detailed_components' ước tính calo/protein riêng cho từng thành phần chính được nhận diện.
            TRẢ VỀ JSON:
            {json_structure}
            CHỈ TRẢ JSON.
            """
        else:
            # PROMPT CƠ BẢN (KHÔNG CÓ detailed_components)
            json_structure = f"""
            {{
            "food_name": "Tên món ăn tiếng Việt",
            "calories": số nguyên,
            "protein": số thực (gram),
            "carbs": số thực (gram),
            "fat": số thực (gram),
            "warning": true/false,
            "burn_time": "Cần chạy X phút" (nếu warning=true, X=calories/10),
            "advice": "Lời khuyên ngắn gọn"
            }}
            """
            prompt = f"""
            Bạn là chuyên gia dinh dưỡng. Phân tích món ăn và trả về JSON:
            {tdee_str}
            Tình trạng bệnh lý: {pathology_input}
            YÊU CẦU: Nếu món ăn có calo cao/không lành mạnh HOẶC không phù hợp với bệnh lý, đặt warning=true và tính burn_time.
            TRẢ VỀ JSON:
            {json_structure}
            CHỈ TRẢ JSON.
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

        # Bổ sung burn_time nếu AI bỏ sót
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
            "CookingTime": "số phút nấu (ví dụ: 30)",
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

# ===================================
# NEW: WORKOUT PLANNER ENDPOINT
# ===================================

@app.route('/generate_workout_plan', methods=['POST'])
def generate_workout_plan():
    try:
        data = request.get_json()
        
        goal = data['goal']  # TangCo, GiamMo, TangSucBen, TongThe
        level = data['level']  # Newbie, Intermediate, Advanced
        duration = data['duration']  # phút/buổi (30, 45, 60)
        frequency = data['frequency']  # ngày/tuần (3-6)
        equipment = data['equipment']  # GymFull, Home, Minimal
        exercises_available = data['exercises']  # List từ ExerciseDB
        weak_bodyparts = data.get('weakBodyParts', [])  # Nhóm cơ yếu
        injuries = data.get('injuries', 'Không')
        
        # Convert goal to Vietnamese
        goal_map = {
            'TangCo': 'Tăng cơ (Hypertrophy)',
            'GiamMo': 'Giảm mỡ (Fat Loss)',
            'TangSucBen': 'Tăng sức bền (Endurance)',
            'TongThe': 'Tổng thể (General Fitness)'
        }
        goal_vn = goal_map.get(goal, goal)
        
        # Convert level
        level_map = {
            'Newbie': 'Người mới bắt đầu',
            'Intermediate': 'Trung cấp',
            'Advanced': 'Nâng cao'
        }
        level_vn = level_map.get(level, level)
        
        # Convert equipment
        equipment_map = {
            'GymFull': 'Phòng gym đầy đủ thiết bị',
            'Home': 'Tại nhà (Bodyweight + Minimal)',
            'Minimal': 'Tối giản (Chỉ tạ đơn/dây kháng lực)'
        }
        equipment_vn = equipment_map.get(equipment, equipment)
        
        # Format exercises list for prompt
        exercises_formatted = "\n".join([
            f"- {ex['name']} (Target: {ex['target']}, Equipment: {ex['equipment']})"
            for ex in exercises_available[:50]  # Limit to 50 exercises
        ])
        
        prompt = f"""
        Bạn là HLV thể hình chuyên nghiệp được chứng nhận NASM-CPT. Tạo chương trình tập luyện 4 tuần chi tiết:
        
        **THÔNG TIN NGƯỜI DÙNG:**
        - Mục tiêu: {goal_vn}
        - Trình độ: {level_vn}
        - Thời gian/buổi: {duration} phút
        - Tần suất: {frequency} ngày/tuần
        - Thiết bị: {equipment_vn}
        - Nhóm cơ yếu (cần tập thêm): {', '.join(weak_bodyparts) if weak_bodyparts else 'Không có'}
        - Chấn thương/Hạn chế: {injuries}
        
        **DANH SÁCH BÀI TẬP CÓ SẴN:**
        {exercises_formatted}
        
        **YÊU CẦU CHI TIẾT:**
        1. **Cấu trúc 4 tuần**: Progressive Overload - tăng dần volume/intensity
           - Tuần 1: Adaptation (Làm quen)
           - Tuần 2: Volume Increase (Tăng khối lượng)
           - Tuần 3: Intensity Peak (Đỉnh cường độ)
           - Tuần 4: Deload (Giảm tải phục hồi)
        
        2. **Mỗi buổi tập phải bao gồm:**
           - Warm-up: 5-7 phút (dynamic stretching, cardio nhẹ)
           - Main workout: Chính (chia theo nhóm cơ hợp lý)
           - Cool-down: 5 phút (static stretching)
        
        3. **Chi tiết bài tập:**
           - Tên bài tập (từ danh sách trên)
           - Sets x Reps (hoặc Time cho plank/cardio)
           - Rest time giữa các set
           - Tempo nếu cần (3-1-3: 3s xuống, 1s pause, 3s lên)
           - Notes: Form cues, tips quan trọng
        
        4. **Nguyên tắc phân chia:**
           - {frequency} ngày/tuần: Luân phiên nhóm cơ (Push/Pull/Legs hoặc Upper/Lower)
           - Tránh tập cùng nhóm cơ 2 ngày liên tiếp
           - Ưu tiên nhóm cơ yếu (nếu có)
        
        5. **Điều chỉnh theo mục tiêu:**
           - Tăng cơ: 8-12 reps, rest 60-90s, compound movements
           - Giảm mỡ: 12-15 reps, rest 30-45s, circuit training
           - Sức bền: 15-20 reps, rest 30s, endurance focus
           - Tổng thể: Mix 8-15 reps, varied rest
        
        6. **An toàn:**
           - Tránh bài tập gây tổn thương nếu có injuries
           - Progressive overload an toàn (không tăng >10%/tuần)
        
        **TRẢ VỀ JSON FORMAT NÀY (QUAN TRỌNG - CHỈ JSON, KHÔNG GIẢI THÍCH):**
        {{
          "planName": "Tên chương trình (VD: 4-Week Muscle Builder)",
          "duration": "4 weeks",
          "goal": "{goal_vn}",
          "level": "{level_vn}",
          "summary": "Mô tả tổng quan chương trình (2-3 câu)",
          "weeks": [
            {{
              "weekNumber": 1,
              "focus": "Adaptation - Làm quen",
              "description": "Tuần đầu tập nhẹ để cơ thể làm quen",
              "days": [
                {{
                  "dayNumber": 1,
                  "focus": "Upper Body Push (Ngực, Vai, Tay sau)",
                  "warmup": "5 phút: Arm circles, Band pull-aparts, Light cardio",
                  "exercises": [
                    {{
                      "name": "Push-ups",
                      "sets": 3,
                      "reps": "10-12",
                      "rest": "60s",
                      "tempo": "3-1-3",
                      "notes": "Giữ core chặt, không võng lưng. Nếu khó, quỳ gối.",
                      "gifUrl": "https://..."
                    }}
                  ],
                  "cooldown": "5 phút: Chest stretch, Shoulder stretch, Deep breathing"
                }}
              ]
            }}
          ],
          "nutritionTips": [
            "Ăn đủ protein: {1.6 if goal == 'TangCo' else 1.2}g/kg cân nặng",
            "Ngủ đủ 7-9 giờ để cơ phục hồi",
            "Uống 3-4L nước/ngày"
          ],
          "progressTracking": "Đo lường: Tăng weight 2.5-5kg khi hoàn thành dễ dàng, hoặc tăng 1-2 reps"
        }}
        
        **LƯU Ý**: CHỈ TRẢ VỀ JSON HỢP LỆ, KHÔNG THÊM BẤT KỲ VĂN BẢN NÀO KHÁC.
        """
        
        response = model.generate_content([prompt])
        raw_text = response.text.strip()
        
        # Extract JSON
        json_match = re.search(r'\{.*\}', raw_text, re.DOTALL)
        if not json_match:
            return jsonify({
                "error": "AI không trả về JSON hợp lệ",
                "raw_response": raw_text[:500]
            }), 500
        
        json_str = json_match.group(0)
        # plan_data = json.loads(json_str)
        
        # return jsonify(plan_data)
        # FIX BUG 5.2 & 5.3 (GIF URL): Tạo map từ tên bài tập đến URL GIF cục bộ
        gif_map = {ex['name'].lower(): ex['gifUrl'] for ex in exercises_available}

        def update_gifs(plan_json, gif_map):
            # Hàm đệ quy để duyệt và sửa GIF URL
            if isinstance(plan_json, dict):
                if 'exercises' in plan_json:
                    for exercise in plan_json['exercises']:
                        name = exercise.get('name', '').lower()
                        if name in gif_map:
                            # Ghi đè URL AI tạo bằng URL cục bộ chính xác
                            exercise['gifUrl'] = gif_map[name]
                for key, value in plan_json.items():
                    plan_json[key] = update_gifs(value, gif_map)
            elif isinstance(plan_json, list):
                return [update_gifs(item, gif_map) for item in plan_json]
            return plan_json

        plan_data = json.loads(json_str)
        plan_data = update_gifs(plan_data, gif_map) # ÁP DỤNG FIX GIF URL
        return jsonify(plan_data)
    
    except Exception as e:
        return jsonify({"error": f"Lỗi Workout AI: {str(e)}"}), 500

# ===================================
# NEW: MEAL PLANNER ENDPOINT
# ===================================

@app.route('/generate_meal_plan', methods=['POST'])
def generate_meal_plan():
    try:
        data = request.get_json()
        daily_calories = data['dailyCalories']
        daily_protein = data['dailyProtein']
        daily_carbs = data['dailyCarbs']
        daily_fat = data['dailyFat']
        duration = data.get('duration', 7)
        budget = data.get('budget', 'Không giới hạn')
        meal_count = data.get('mealCount', 3)
        dietary_restrictions = data.get('dietaryRestrictions', 'Không')
        goal = data.get('goal', 'Duy trì')

        if duration > 7:
            duration = 7
        
        # --- SỬA ĐỔI 1: Tối ưu Prompt để giảm lượng Token ---
        prompt = f"""
Tạo thực đơn {duration} ngày.
NGẮN GỌN, CHỈ THÔNG TIN CẦN THIẾT ĐỂ GIẢM TOKEN.

**MỤC TIÊU/NGÀY:** {daily_calories} kcal | P:{daily_protein}g C:{daily_carbs}g F:{daily_fat}g
**SỐ BỮA:** {meal_count} | **NGÂN SÁCH:** {budget} | **HẠN CHẾ:** {dietary_restrictions}

**YÊU CẦU:**
- Món ăn Việt Nam, dễ nấu
- **Instructions**: RẤT NGẮN GỌN (dưới 10 từ).
- JSON Format chuẩn.

**JSON FORMAT (CHỈ TRẢ JSON):**
{{
  "planName": "Thực đơn {duration} ngày",
  "totalDays": {duration},
  "dailyTarget": {{"calories": {daily_calories}, "protein": {daily_protein}, "carbs": {daily_carbs}, "fat": {daily_fat}}},
  "days": [
    {{
      "dayNumber": 1,
      "meals": [
        {{
          "mealType": "Bữa Sáng",
          "dishName": "Phở gà",
          "ingredients": ["Phở", "Gà", "Rau"],
          "instructions": "Luộc gà, chan nước dùng.", 
          "calories": 450,
          "protein": 30,
          "carbs": 55,
          "fat": 8,
          "prepTime": "20m",
          "cost": "25k"
        }}
      ],
      "dailyTotal": {{"calories": {daily_calories}, "protein": {daily_protein}, "carbs": {daily_carbs}, "fat": {daily_fat}}}
    }}
  ],
  "groceryList": [
    {{"item": "Gà", "quantity": "500g", "estimatedCost": "40k"}}
  ],
  "totalEstimatedCost": "300k"
}}
"""
        
        # --- SỬA ĐỔI 2: Tăng Max Output Tokens ---
        generation_config = genai.types.GenerationConfig(
            temperature=0.7,
            # Tăng từ 4096 lên 8192 (Gemini 1.5 Flash hỗ trợ output lớn hơn)
            max_output_tokens=8192 
        )
        
        response = model.generate_content(
            [prompt],
            generation_config=generation_config,
            request_options={'timeout': 120} # Tăng timeout lên 120s phòng khi mạng chậm
        )
        
        # --- SỬA ĐỔI 3: Kiểm tra Safety & Finish Reason trước khi lấy text ---
        if response.prompt_feedback and response.prompt_feedback.block_reason:
             return jsonify({"error": f"AI Blocked: {response.prompt_feedback.block_reason}"}), 500

        # Kiểm tra nếu response có candidates
        if not response.candidates:
             return jsonify({"error": "AI không trả về kết quả nào."}), 500

        # Truy cập text an toàn hơn
        try:
            raw_text = response.text.strip()
        except Exception as e:
            # Nếu truy cập .text lỗi, kiểm tra parts
            if response.candidates[0].finish_reason == 2: # MAX_TOKENS
                 return jsonify({"error": "Kế hoạch quá dài, AI bị ngắt quãng. Vui lòng giảm số ngày (VD: 3 ngày)."}), 500
            return jsonify({"error": f"Lỗi đọc phản hồi AI: {str(e)}"}), 500

        json_match = re.search(r'\{.*\}', raw_text, re.DOTALL)
        
        if not json_match:
            return jsonify({"error": "AI không trả JSON hợp lệ", "raw": raw_text[:300]}), 500
        
        json_str = json_match.group(0)
        try:
            plan_data = json.loads(json_str)
        except json.JSONDecodeError:
             return jsonify({"error": "JSON bị lỗi cú pháp (có thể do bị cắt giữa chừng)", "raw": raw_text[-200:]}), 500
        
        return jsonify(plan_data)
        
    except Exception as e:
        print(f"Server Error: {str(e)}") # Log lỗi ra console server
        return jsonify({"error": f"Lỗi Meal Planner: {str(e)}"}), 500

if __name__ == '__main__':
    print("Flask AI Service: http://localhost:5000")
    print("Model:", MODEL_NAME)
    print("NEW Endpoints:")
    print("  - POST /generate_workout_plan")
    print("  - POST /generate_meal_plan")
    app.run(host='0.0.0.0', port=5000, debug=True)