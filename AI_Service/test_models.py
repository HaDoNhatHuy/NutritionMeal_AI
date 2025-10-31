import google.generativeai as genai
from dotenv import load_dotenv
import os

load_dotenv()
API_KEY = os.getenv("GEMINI_API_KEY")
if not API_KEY:
    print("LỖI: Không tìm thấy GEMINI_API_KEY trong .env")
    exit()

genai.configure(api_key=API_KEY)

# Liệt kê tất cả model
for model in genai.list_models():
    print(f"Model: {model.name} - Hỗ trợ generateContent: {model.supported_generation_methods}")
    if 'gemini-1.5' in model.name:
        print(f"  → CHI TIẾT: {model.name}")

print("\nKết thúc. Tìm model có 'gemini-1.5-flash-latest' hoặc 'gemini-1.5-pro-latest'.")