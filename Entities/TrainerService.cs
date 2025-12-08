namespace GymProject1.Entities
{
    public class TrainerService
    {
        public int TrainerId { get; set; }
        public Trainer? Trainer { get; set; }

        public int ServiceId { get; set; }
        public Service? Service { get; set; }
    }
}
