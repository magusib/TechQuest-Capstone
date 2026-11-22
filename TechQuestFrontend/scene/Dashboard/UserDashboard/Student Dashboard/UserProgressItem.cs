using Godot;

public partial class UserProgressItem : PanelContainer
{
    public void SetItem(UserProgressItemDto item)
    {
        GetNode<Label>("CardMargin/CardVBox/SubjectLabel").Text = "Subject: " + item.Subject;
        GetNode<RichTextLabel>("CardMargin/CardVBox/QuestionLabel").Text = "Question: " + item.Question;
        GetNode<RichTextLabel>("CardMargin/CardVBox/CorrectAnswerLabel").Text = "Correct Answer: " + item.CorrectAnswer;
        GetNode<Label>("CardMargin/CardVBox/StatusLabel").Text = "Status: " + item.Status;
        GetNode<Label>("CardMargin/CardVBox/RewardsContainer/CoinsEarnedLabel").Text = "Coins Earned: " + item.CoinsEarned;
        GetNode<Label>("CardMargin/CardVBox/RewardsContainer/PointsEarnedLabel").Text = "Points Earned: " + item.PointsEarned;
    }
}
