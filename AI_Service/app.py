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

@app.route('/analyze', methods=['POST'])
def analyze_food():
    if 'image' not in request.files:
        return jsonify({"error": "Không có ảnh"}), 400

    file = request.files['image']
    if file.filename == '':
        return jsonify({"error": "Tên file rỗng"}), 400

    try:
        # Đọc ảnh
        img_bytes = file.read()
        img = Image.open(io.BytesIO(img_bytes))

        # PROMPT SIÊU CHÍNH XÁC – BẮT BUỘC JSON
        prompt = """
        Bạn là chuyên gia dinh dưỡng. Phân tích ảnh món ăn và trả về đúng định dạng JSON sau, KHÔNG thêm bất kỳ giải thích nào:

        {
          "food_name": "Tên món ăn bằng tiếng Việt (ví dụ: Phở bò, Trà sữa trân châu)",
          "calories": số nguyên (calo cho 1 phần ăn trong ảnh),
          "protein": số thực (gram),
          "carbs": số thực (gram),
          "fat": số thực (gram),
          "warning": true hoặc false (true nếu là đồ ăn vặt, trà sữa, chiên rán, nhiều đường),
          "burn_time": "Cần chạy bộ X phút để tiêu hao" (chỉ nếu warning=true, X = calories / 10)
        }

        Ví dụ:
        {
          "food_name": "Trà sữa trân châu",
          "calories": 450,
          "protein": 5.0,
          "carbs": 80.0,
          "fat": 12.0,
          "warning": true,
          "burn_time": "Cần chạy bộ 45 phút để tiêu hao"
        }

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

if __name__ == '__main__':
    print("Flask AI Service đang chạy tại: http://localhost:5000")
    print("Dùng model:", MODEL_NAME)
    app.run(host='0.0.0.0', port=5000, debug=True)