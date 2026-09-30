using System;
using System.Collections.Generic;

public static class MultiplayerRoomState
{
	public static int RoomId { get; set; }
	public static int StudentId { get; set; }
	public static string RoomCode { get; set; } = "";
	public static string RoomName { get; set; } = "";
	public static int TimerSeconds { get; set; } = 30;
	public static List<RoomQuizQuestionDto> Questions { get; set; } = new();
	public static List<RoomAnswerDto> Answers { get; set; } = new();
	public static int Score { get; set; }
	public static bool ReturnToWaitingRoom { get; set; }

	public static void SetRoom(int roomId, string roomCode, string roomName, int studentId = 0)
	{
		RoomId = roomId;
		RoomCode = roomCode ?? "";
		RoomName = roomName ?? "";

		if (studentId > 0)
			StudentId = studentId;
	}

	public static void SetQuiz(int timerSeconds, List<RoomQuizQuestionDto> questions)
	{
		TimerSeconds = Math.Max(1, timerSeconds);
		Questions = questions ?? new List<RoomQuizQuestionDto>();
		Answers = new List<RoomAnswerDto>();
		Score = 0;
	}

	public static void Clear()
	{
		RoomId = 0;
		StudentId = 0;
		RoomCode = "";
		RoomName = "";
		TimerSeconds = 30;
		Questions.Clear();
		Answers.Clear();
		Score = 0;
		ReturnToWaitingRoom = false;
	}
}

public class RoomSummaryDto
{
	public int RoomId { get; set; }
	public string RoomCode { get; set; } = "";
	public string RoomName { get; set; } = "";
	public string Status { get; set; } = "";
	public int StudentCount { get; set; }
}

public class RoomJoinDto : RoomSummaryDto
{
	public int StudentId { get; set; }
}

public class RoomParticipantDto
{
	public int StudentId { get; set; }
	public string Name { get; set; } = "";
	public string Avatar { get; set; } = "";
}

public class RoomQuizDto
{
	public int TimerSeconds { get; set; }
	public List<RoomQuizQuestionDto> Questions { get; set; } = new();
}

public class RoomQuizQuestionDto
{
	public string Type { get; set; } = "";
	public string SubjectName { get; set; } = "";
	public string Question { get; set; } = "";
	public string ChoiceA { get; set; } = "";
	public string ChoiceB { get; set; } = "";
	public string ChoiceC { get; set; } = "";
	public string ChoiceD { get; set; } = "";
	public string CorrectAnswer { get; set; } = "";
}

public class RoomAnswerDto : RoomQuizQuestionDto
{
	public string StudentAnswer { get; set; } = "";
	public bool IsCorrect { get; set; }
}

public class RoomResultDto
{
	public int Score { get; set; }
	public int TotalQuestions { get; set; }
	public List<RoomAnswerDto> Answers { get; set; } = new();
}
