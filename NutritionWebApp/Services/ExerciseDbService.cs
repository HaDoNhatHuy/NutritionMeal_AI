//using System.Text.Json;

//namespace NutritionWebApp.Services
//{
//    public class ExerciseDbService : IExerciseDbService
//    {
//        private readonly HttpClient _httpClient;
//        private const string BASE_URL = "https://exercisedb.p.rapidapi.com";

//        // 🔑 QUAN TRỌNG: Đăng ký RapidAPI (miễn phí) để lấy key
//        // Link: https://rapidapi.com/justin-WFnsXH_t6/api/exercisedb
//        private const string API_KEY = "100096b7f4mshdaff7ca47d59270p1c4c00jsn04487dbe2227"; // THAY BẰNG KEY CỦA BẠN

//        public ExerciseDbService(HttpClient httpClient)
//        {
//            _httpClient = new HttpClient
//            {
//                Timeout = TimeSpan.FromSeconds(300) // Đặt timeout 300 giây
//            };
//            _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", API_KEY);
//            _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", "exercisedb.p.rapidapi.com");
//        }

//        public async Task<List<Exercise>> GetExercisesByBodyPart(string bodyPart)
//        {
//            try
//            {
//                var response = await _httpClient.GetAsync($"{BASE_URL}/exercises/bodyPart/{bodyPart}");
//                response.EnsureSuccessStatusCode();

//                var json = await response.Content.ReadAsStringAsync();
//                var exercises = JsonSerializer.Deserialize<List<Exercise>>(json, new JsonSerializerOptions
//                {
//                    PropertyNameCaseInsensitive = true
//                });

//                return exercises ?? new List<Exercise>();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error fetching exercises: {ex.Message}");
//                return new List<Exercise>();
//            }
//        }

//        public async Task<List<Exercise>> GetExercisesByEquipment(string equipment)
//        {
//            try
//            {
//                var response = await _httpClient.GetAsync($"{BASE_URL}/exercises/equipment/{equipment}");
//                response.EnsureSuccessStatusCode();

//                var json = await response.Content.ReadAsStringAsync();
//                var exercises = JsonSerializer.Deserialize<List<Exercise>>(json, new JsonSerializerOptions
//                {
//                    PropertyNameCaseInsensitive = true
//                });

//                return exercises ?? new List<Exercise>();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error fetching exercises: {ex.Message}");
//                return new List<Exercise>();
//            }
//        }

//        public async Task<List<Exercise>> GetExercisesByTarget(string target)
//        {
//            try
//            {
//                var response = await _httpClient.GetAsync($"{BASE_URL}/exercises/target/{target}");
//                response.EnsureSuccessStatusCode();

//                var json = await response.Content.ReadAsStringAsync();
//                var exercises = JsonSerializer.Deserialize<List<Exercise>>(json, new JsonSerializerOptions
//                {
//                    PropertyNameCaseInsensitive = true
//                });

//                return exercises ?? new List<Exercise>();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error fetching exercises: {ex.Message}");
//                return new List<Exercise>();
//            }
//        }

//        public async Task<List<string>> GetBodyPartList()
//        {
//            try
//            {
//                var response = await _httpClient.GetAsync($"{BASE_URL}/exercises/bodyPartList");
//                response.EnsureSuccessStatusCode();

//                var json = await response.Content.ReadAsStringAsync();
//                var bodyParts = JsonSerializer.Deserialize<List<string>>(json);

//                return bodyParts ?? new List<string>();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error fetching body parts: {ex.Message}");
//                return new List<string> { "back", "cardio", "chest", "lower arms", "lower legs", "neck", "shoulders", "upper arms", "upper legs", "waist" };
//            }
//        }

//        public async Task<List<string>> GetEquipmentList()
//        {
//            try
//            {
//                var response = await _httpClient.GetAsync($"{BASE_URL}/exercises/equipmentList");
//                response.EnsureSuccessStatusCode();

//                var json = await response.Content.ReadAsStringAsync();
//                var equipment = JsonSerializer.Deserialize<List<string>>(json);

//                return equipment ?? new List<string>();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error fetching equipment: {ex.Message}");
//                return new List<string> { "body weight", "barbell", "dumbbell", "cable", "machine" };
//            }
//        }
//    }

//    // Exercise Model
//    public class Exercise
//    {
//        public string Id { get; set; } = string.Empty;
//        public string Name { get; set; } = string.Empty;
//        public string BodyPart { get; set; } = string.Empty;
//        public string Target { get; set; } = string.Empty;
//        public string Equipment { get; set; } = string.Empty;
//        public string GifUrl { get; set; } = string.Empty;
//        public List<string> Instructions { get; set; } = new List<string>();
//        public List<string>? SecondaryMuscles { get; set; }
//    }
//}

using System.Text.Json;
using Microsoft.AspNetCore.Hosting; // Cần namespace này để lấy đường dẫn thư mục
using NutritionWebApp.Services; // Đảm bảo namespace đúng

namespace NutritionWebApp.Services
{
    public class ExerciseDbService : IExerciseDbService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private List<Exercise> _allExercises;
        private List<string> _allBodyParts; // <-- Thêm
        private List<string> _allEquipment;  // <-- Thêm
        private List<string> _allMuscles;  // <-- Thêm
        private List<string> _allTargets;

        // Inject IWebHostEnvironment để truy cập thư mục wwwroot
        public ExerciseDbService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _allExercises = new List<Exercise>(); // Khởi tạo list rỗng
            LoadExercises(); // Tải dữ liệu ngay khi khởi tạo Service
            LoadMetaData(); // <-- Gọi thêm hàm này
        }
        // Class phụ để hứng dữ liệu {"name": "value"}
        private class NamedItem { public string Name { get; set; } = ""; }
        // Hàm mới để đọc các file nhỏ
        private List<string> ReadJsonList(string fileName)
        {
            try
            {
                var path = Path.Combine(_webHostEnvironment.WebRootPath, "data", fileName);
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    // Đọc danh sách object [{"name": "chest"}, ...]
                    var items = JsonSerializer.Deserialize<List<NamedItem>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    // Chỉ lấy ra list tên: ["chest", ...]
                    return items?.Select(x => x.Name).ToList() ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading {fileName}: {ex.Message}");
            }
            return new List<string>();
        }

        private void LoadMetaData()
        {
            // Đảm bảo tên file khớp chính xác với file trong thư mục wwwroot/data
            _allBodyParts = ReadJsonList("bodyParts.json");
            _allEquipment = ReadJsonList("equipments.json");
            _allTargets = ReadJsonList("muscles.json");

            // Fallback nếu file không đọc được để test giao diện
            if (!_allBodyParts.Any()) _allBodyParts = new List<string> { "back", "cardio", "chest", "lower arms", "lower legs", "neck", "shoulders", "upper arms", "upper legs", "waist" };
            if (!_allEquipment.Any()) _allEquipment = new List<string> { "body weight", "cable", "dumbbell", "barbell", "band" };
            if (!_allTargets.Any()) _allTargets = new List<string> { "abs", "quads", "lats", "calves", "pectorals", "glutes" };
        }
        private void LoadExercises()
        {
            try
            {
                var path = Path.Combine(_webHostEnvironment.WebRootPath, "data", "exercises.json");

                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    _allExercises = JsonSerializer.Deserialize<List<Exercise>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Exercise>();

                    // --- ĐOẠN CODE QUAN TRỌNG CẦN THÊM ---
                    // Tự động sửa đường dẫn ảnh để trỏ vào thư mục local
                    foreach (var ex in _allExercises)
                    {
                        // Giả sử file ảnh tên là "0001.gif" tương ứng với Id "0001"
                        // Đường dẫn sẽ là: /images/exercises/0001.gif
                        ex.GifUrl = $"/images/exercises/gifs_360x360/{ex.GifUrl}";
                    }
                    // ---------------------------------------
                }
                else
                {
                    _allExercises = new List<Exercise>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đọc file JSON: {ex.Message}");
                _allExercises = new List<Exercise>();
            }
        }

        // Lọc bài tập theo nhóm cơ (Body Part)
        public Task<List<Exercise>> GetExercisesByBodyPart(string bodyPart)
        {
            if (string.IsNullOrEmpty(bodyPart) || bodyPart.ToLower() == "all")
            {
                // Trả về tất cả (sẽ được WorkoutController giới hạn 200)
                return Task.FromResult(_allExercises);
            }

            var bodyPartLower = bodyPart.ToLower();

            // FIX: Sử dụng BodyPartsList và kiểm tra xem có bất kỳ phần tử nào khớp không
            var result = _allExercises
                .Where(e => e.BodyPartsList != null &&
                            e.BodyPartsList.Any(bp => bp.ToLower() == bodyPartLower))
                .ToList();

            return Task.FromResult(result);
        }

        // Lọc theo thiết bị (Equipment)
        public Task<List<Exercise>> GetExercisesByEquipment(string equipment)
        {
            var result = _allExercises
               .Where(e => e.Equipment != null && e.Equipment.ToLower().Contains(equipment.ToLower()))
               .ToList();
            return Task.FromResult(result);
        }

        // Lọc theo cơ bắp mục tiêu (Target)
        public Task<List<Exercise>> GetExercisesByTarget(string target)
        {
            var result = _allExercises
               .Where(e => e.Target != null && e.Target.ToLower().Contains(target.ToLower()))
               .ToList();
            return Task.FromResult(result);
        }

        // Lấy danh sách các nhóm cơ (để hiển thị menu lọc)
        // --- 2. CẬP NHẬT HÀM LẤY DANH SÁCH ---
        public Task<List<string>> GetBodyPartList() => Task.FromResult(_allBodyParts);
        public Task<List<string>> GetEquipmentList() => Task.FromResult(_allEquipment);
        public Task<List<string>> GetMusclesList() => Task.FromResult(_allMuscles);
        public Task<List<string>> GetTargetList() => Task.FromResult(_allTargets);

        // --- 3. HÀM LỌC TỔNG HỢP (QUAN TRỌNG) ---
        public Task<List<Exercise>> FilterExercises(string bodyPart, string equipment, string target)
        {
            var query = _allExercises.AsEnumerable();

            // Lọc theo Body Part
            if (!string.IsNullOrEmpty(bodyPart) && bodyPart != "all")
            {
                query = query.Where(e => e.BodyPartsList.Any(bp => bp.Equals(bodyPart, StringComparison.OrdinalIgnoreCase)));
            }

            // Lọc theo Equipment
            if (!string.IsNullOrEmpty(equipment) && equipment != "all")
            {
                query = query.Where(e => e.Equipment.Equals(equipment, StringComparison.OrdinalIgnoreCase));
            }

            // Lọc theo Target
            if (!string.IsNullOrEmpty(target) && target != "all")
            {
                query = query.Where(e => e.TargetMusclesList.Any(t => t.Equals(target, StringComparison.OrdinalIgnoreCase)));
            }

            return Task.FromResult(query.ToList());
        }
    }

    // Giữ nguyên Class Exercise Model ở cuối file (nếu chưa có file riêng)
    // Nếu bạn đã tách Exercise ra file riêng trong Models thì xóa đoạn dưới này đi

    public class Exercise
    {
        // --- FIX: Thêm mapping cho exerciseId ---
        [System.Text.Json.Serialization.JsonPropertyName("exerciseId")]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Equipment { get; set; } = string.Empty;
        public string GifUrl { get; set; } = string.Empty;
        public List<string> Instructions { get; set; } = new List<string>();
        public List<string> SecondaryMuscles { get; set; } = new List<string>();

        [System.Text.Json.Serialization.JsonPropertyName("bodyParts")]
        public List<string> BodyPartsList { get; set; } = new List<string>();

        [System.Text.Json.Serialization.JsonPropertyName("targetMuscles")]
        public List<string> TargetMusclesList { get; set; } = new List<string>();

        // Helper properties
        public string BodyPart => BodyPartsList.FirstOrDefault() ?? "N/A";
        public string Target => TargetMusclesList.FirstOrDefault() ?? "N/A";
    }

}