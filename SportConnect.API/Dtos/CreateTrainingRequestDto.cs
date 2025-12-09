using SportConnect.API.Models;
using System.ComponentModel.DataAnnotations;

public class CreateTrainingRequestDto
{
    public Guid ReceiverId { get; set; }
}

public class UpdateTrainingRequestStatusDto
{
    [Required(ErrorMessage = "TrainingRequestStatusRequired")]
    public TrainingRequestStatus Status { get; set; }
}

