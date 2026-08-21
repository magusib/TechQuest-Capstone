#nullable enable

using Godot;
using System;

public partial class AccountSession : Node
{
    public static AccountSession? Current { get; private set; }

    public int UserId { get; private set; }
    public string Role { get; private set; } = "";
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public int Coins { get; private set; }
    public int Points { get; private set; }
    public string AvatarPath { get; private set; } = "";

    public override void _EnterTree()
    {
        Current = this;
    }

    public override void _ExitTree()
    {
        if (Current == this)
        {
            Current = null;
        }
    }

    public void SetAccount(int userId, string role, string firstName = "", string lastName = "", int coins = 0, int points = 0, string avatarPath = "")
    {
        UserId = userId;
        Role = NormalizeRole(role);
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Coins = coins;
        Points = points;
        AvatarPath = avatarPath.Trim();
    }

    public bool HasRole(string role)
    {
        return string.Equals(Role, NormalizeRole(role), StringComparison.OrdinalIgnoreCase);
    }

    public void Clear()
    {
        UserId = 0;
        Role = "";
        FirstName = "";
        LastName = "";
        Coins = 0;
        Points = 0;
        AvatarPath = "";
    }

    public static string NormalizeRole(string role)
    {
        return role.Trim().ToLowerInvariant() switch
        {
            "student" => "student",
            "professor" => "professor",
            "admin" or "administrator" => "admin",
            _ => ""
        };
    }
}
