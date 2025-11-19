namespace NutritionWebApp.Services
{
    public interface IExerciseDbService
    {
        Task<List<Exercise>> GetExercisesByBodyPart(string bodyPart);
        Task<List<Exercise>> GetExercisesByEquipment(string equipment);
        Task<List<Exercise>> GetExercisesByTarget(string target);
        Task<List<string>> GetBodyPartList();
        Task<List<string>> GetEquipmentList();
        Task<List<string>> GetTargetList();
        Task<List<Exercise>> FilterExercises(string bodyPart, string equipment, string target);
    }
}
