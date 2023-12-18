namespace SharedProject.Models;

public class User
{
    public uint Baid { get; set; }

    public string Name { get; set; } = new("");
    
    public List<string> AccessCodes { get; set; } = new();
    
    public bool IsAdmin { get; set; }
}