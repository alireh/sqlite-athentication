using Athentication.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Athentication.Util;
using Newtonsoft.Json;

namespace Athentication.Controllers
{
    public class MainController : Controller
    {
        public static ILogger<MainController> logger;
        [HttpGet]
        [Route("/index")]
        /// <summary>
        /// Api For simple Test connection
        /// </summary>
        /// <returns>Simple test string</returns>
        public string Index()
        {
            return "Sqlite Authentication";
        }

        /// <summary>
        /// Api for add one user
        /// </summary>
        /// <param name="user">user infos </param>
        /// <returns>Response Code and message.</returns>
        [HttpPost]
        [Route("/api/authentication/signup")]
        public async Task<ResponseModel> Signup([FromBody] UserRawInfo userInfo)
        {
            return await Task.Run(() =>
            {
                try
                {
                    logger?.LogInformation("Signup");
                    if (string.IsNullOrWhiteSpace(userInfo.Firstname) || string.IsNullOrWhiteSpace(userInfo.Lastname) || string.IsNullOrWhiteSpace(userInfo.Username) || string.IsNullOrWhiteSpace(userInfo.Password))
                    {
                        return new ResponseModel
                        {
                            message = "bad input",
                            status = 400
                        };
                    }
                    if (SqliteManager.Instance.ExistsUser(userInfo.Username))
                    {
                        return new ResponseModel
                        {
                            message = "already exists",
                            status = 409
                        };
                    }

                    var id = SqliteManager.Instance.AddUser(userInfo);

                    var now = DateTime.Now;
                    SqliteManager.Instance.AddReport(new ReportInfo
                    {
                        State = "Signup",
                        ActionDate = $"{now.Year}-{now.Month}-{now.Day} : {now.Hour}:{now.Minute}:{now.Second}",
                        Username = userInfo.Username,
                        UserId = id,
                    });

                    return new ResponseModel
                    {
                        message = "success",
                        status = 200
                    };
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Signup Exception Message : {ex.Message}");
                    return new ResponseModel
                    {
                        message = "error",
                        status = 500
                    };
                }
            });
        }

        /// <summary>
        /// Api for edit one user
        /// </summary>
        /// <param name="user">user infos </param>
        /// <returns>Response Code and message.</returns>
        [HttpPost]
        [Route("/api/authentication/edit")]
        public async Task<ResponseModel> EditUser([FromBody] UserInfo userInfo)
        {
            return await Task.Run(() =>
            {
                try
                {
                    logger?.LogInformation("EditUser");

                    SqliteManager.Instance.EditUser(userInfo);
                    return new ResponseModel
                    {
                        message = "success",
                        status = 200
                    };
                }
                catch (Exception ex)
                {
                    logger?.LogError($"EditUser Exception Message : {ex.Message}");
                    return new ResponseModel
                    {
                        message = "error",
                        status = 500
                    };
                }
            });
        }

        /// <summary>
        /// Returns a user list
        /// </summary>
        /// <param name="userInfo">The user informations </param>
        /// <returns>Response Code, Data and message.</returns>
        [HttpGet]
        [Route("/api/authentication/signin")]
        public async Task<ResponseModel> Signin([FromBody] UserAuthInfo userInfo)
        {
            return await Task.Run(() =>
            {
                try
                {
                    logger?.LogInformation("Signin");
                    var exists = SqliteManager.Instance.ExistsUser(userInfo.Username, userInfo.Password);
                    if (exists)
                    {
                        var now = DateTime.Now;
                        var user = SqliteManager.Instance.GetUser(userInfo.Username);
                        SqliteManager.Instance.AddReport(new ReportInfo
                        {
                            State = "Signin",
                            ActionDate = $"{now.Year}-{now.Month}-{now.Day} : {now.Hour}:{now.Minute}:{now.Second}",
                            Username = user.Username,
                            UserId = user.Id,
                        });
                        return new ResponseModel
                        {
                            message = "success",
                            status = 200
                        };
                    }
                    return new ResponseModel
                    {
                        message = "failed",
                        status = 404
                    };
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Signin Message : {ex.Message}");
                    return new ResponseModel
                    {
                        message = "failed",
                        status = 500
                    };
                }
            });
        }

        /// <summary>
        /// Signout user
        /// </summary>
        /// <returns>Response Code, Data and message.</returns>
        [HttpGet]
        [Route("/api/authentication/signout/{id}")]
        public async Task<ResponseModel> Signout([FromRoute] int id)
        {
            return await Task.Run(() =>
            {
                try
                {
                    logger?.LogInformation("Signout");
                    var success = SqliteManager.Instance.UpdateUserToken(id, "");
                    if (success)
                    {
                        var now = DateTime.Now;
                        var user = SqliteManager.Instance.GetUser(id);
                        SqliteManager.Instance.AddReport(new ReportInfo
                        {
                            State = "Signout",
                            ActionDate = $"{now.Year}-{now.Month}-{now.Day} : {now.Hour}:{now.Minute}:{now.Second}",
                            Username = user.Username,
                            UserId = user.Id,
                        });
                        return new ResponseModel
                        {
                            message = "log out success",
                            status = 200
                        };
                    }
                    return new ResponseModel
                    {
                        message = "failed",
                        status = 500
                    };
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Signout Message : {ex.Message}");
                    return new ResponseModel
                    {
                        message = "failed",
                        status = 500
                    };
                }
            });
        }

        /// <summary>
        /// Returns a user list
        /// </summary>
        /// <returns>Response Code, Data and message.</returns>
        [HttpGet]
        [Route("/api/authentication/users")]
        public async Task<ResponseModel> GetUsers()
        {
            return await Task.Run(() =>
            {
                try
                {
                    logger?.LogInformation("GetUsers");

                    var users = SqliteManager.Instance.GetUsers();
                    var data = JsonConvert.SerializeObject(users);
                    return new ResponseModel
                    {
                        content = data,
                        message = "success",
                        status = 200
                    };
                }
                catch (Exception ex)
                {
                    logger?.LogError($"GetUsers Message : {ex.Message}");
                    return new ResponseModel
                    {
                        message = "failed",
                        status = 500
                    };
                }
            });
        }

        /// <summary>
        /// Returns a user
        /// </summary>
        /// <returns>Response Code, Data and message.</returns>
        [HttpGet]
        [Route("/api/authentication/user/{id}")]
        public async Task<ResponseModel> GetUser([FromRoute] int id)
        {
            return await Task.Run(() =>
            {
                try
                {
                    logger?.LogInformation("GetUser");

                    var user = SqliteManager.Instance.GetUser(id);
                    if (user != null)
                    {
                        var data = JsonConvert.SerializeObject(user);
                        return new ResponseModel
                        {
                            content = data,
                            message = "success",
                            status = 200
                        };
                    }
                    return new ResponseModel
                    {
                        content = null,
                        message = "not exists",
                        status = 404
                    };
                }
                catch (Exception ex)
                {
                    logger?.LogError($"GetUser Message : {ex.Message}");
                    return new ResponseModel
                    {
                        message = "failed",
                        status = 500
                    };
                }
            });
        }

        /// <summary>
        /// Returns a report list
        /// </summary>
        /// <returns>Response Code, Data and message.</returns>
        [HttpGet]
        [Route("/api/authentication/reports")]
        public async Task<ResponseModel> GetReports()
        {
            return await Task.Run(() =>
            {
                try
                {
                    logger?.LogInformation("GetReports");

                    var reports = SqliteManager.Instance.GetReports();
                    var data = JsonConvert.SerializeObject(reports);
                    return new ResponseModel
                    {
                        content = data,
                        message = "success",
                        status = 200
                    };
                }
                catch (Exception ex)
                {
                    logger?.LogError($"GetReports Message : {ex.Message}");
                    return new ResponseModel
                    {
                        message = "failed",
                        status = 500
                    };
                }
            });
        }
    }
}
