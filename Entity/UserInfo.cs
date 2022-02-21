namespace Athentication.Entity
{
    public class UserInfo
    {
        public UserInfo(string username, string firstname, string lastname, string email, string password, int age, bool? gender, string token)
        {
            Username = username;
            Firstname = firstname;
            Lastname = lastname;
            Email = email;
            Password = password;
            Age = age;
            Gender = gender;
            Token = token;
        }
        public int Id { get; set; }
        public string Username { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int Age { get; set; }
        public bool? Gender { get; set; }
        public string Token { get; set; }
    }
    public class UserAuthInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class UserRawInfo
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int Age { get; set; }
        public bool? Gender { get; set; }
        public string Token { get; set; }
    }
}
