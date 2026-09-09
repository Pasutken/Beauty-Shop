using System;
using System.Collections.Generic;

namespace Project.Models;

public partial class User
{
    public int UsId { get; set; }

    public string UsName { get; set; } = null!;

    public string UsLastname { get; set; } = null!;

    public string? UsPhone { get; set; }

    public string? UsEmail { get; set; }

    public string UsPassword { get; set; } = null!;

    public string? UsAddress { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int RoleId { get; set; }

    public virtual Role Role { get; set; } = null!;
}
