﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VTH.Models;

namespace VTH.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration configuration;
        private readonly string sqlConnectionString;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            this.configuration = configuration;
            this.sqlConnectionString = this.configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            int qno = 0;
            if (this.User.Identity.IsAuthenticated)
            {
                using (SqlConnection connection = new SqlConnection(this.sqlConnectionString))
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    using (var sqlcommand = connection.CreateCommand())
                    {
                        sqlcommand.CommandText = $"GetUserQuestion";
                        sqlcommand.CommandType = System.Data.CommandType.StoredProcedure;
                        sqlcommand.Parameters.Add("@userid", System.Data.SqlDbType.NVarChar).Value = this.User.Identity.Name;
                        var result = await sqlcommand.ExecuteScalarAsync().ConfigureAwait(false);
                        qno = result != null ? (int)result : 1;
                    }
                }
            }
            return View(qno);
        }

        public async Task<ActionResult> Privacy()
        {
            IList<Models.EventLog> logs = new List<Models.EventLog>();
            using (SqlConnection connection = new SqlConnection(this.sqlConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using var sqlcommand = connection.CreateCommand();
                sqlcommand.CommandText = "GetLeaderboard";
                sqlcommand.CommandType = System.Data.CommandType.StoredProcedure;
                using (var reader = await sqlcommand.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (reader.HasRows)
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var log = new Models.EventLog();
                            log.UserId = reader.GetString(0);
                            log.Qno = reader.GetInt32(1);
                            logs.Add(log);
                        }
                        await reader.NextResultAsync().ConfigureAwait(false);
                    }
                }
            }
            return View(logs);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<ActionResult> GetImageAsync(int id)
        {
            string connectionString = this.configuration.GetConnectionString("BlobConnection");
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient("questions");
            var blobClient = containerClient.GetBlobClient($"{id}.png");
            byte[] bytes;
            using(MemoryStream stream = new MemoryStream())
            {
                if(await blobClient.ExistsAsync().ConfigureAwait(false))
                {
                    await blobClient.DownloadToAsync(stream).ConfigureAwait(false);
                    stream.Position = 0;
                    bytes = stream.ToArray();
                }
                else
                {
                    blobClient = containerClient.GetBlobClient($"{id}.jpg");
                    if (await blobClient.ExistsAsync().ConfigureAwait(false))
                    {
                        await blobClient.DownloadToAsync(stream).ConfigureAwait(false);
                        stream.Position = 0;
                        bytes = stream.ToArray();
                    }
                    else
                    {
                        blobClient = containerClient.GetBlobClient("Game-over-2.png");
                        await blobClient.DownloadToAsync(stream).ConfigureAwait(false);
                        stream.Position = 0;
                        bytes = stream.ToArray();
                    }
                }
            }
            return File(bytes, "image/png", "image.png");
        }

        [HttpGet]
        public string GetText(int id)
        {
            
            var name = this.User?.Identity?.Name;
            return name;
        }

        [HttpPost]
        public async Task<bool> ValidateAnswer([FromBody]QnAEntity entity)
        {
            string answer;
            using (SqlConnection connection = new SqlConnection(this.sqlConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (SqlCommand sqlcommand = connection.CreateCommand())
                {
                    sqlcommand.CommandText = "GetAnswers";
                    sqlcommand.CommandType = System.Data.CommandType.StoredProcedure;
                    sqlcommand.Parameters.Add("@qno", System.Data.SqlDbType.Int).Value = entity.Qno;
                    answer = (string)await sqlcommand.ExecuteScalarAsync().ConfigureAwait(false);
                }

                if(string.Equals(answer, entity.Answer, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    using (var newcommand = connection.CreateCommand())
                    {
                        newcommand.CommandText = "UpdateLeaderboard";
                        newcommand.CommandType = System.Data.CommandType.StoredProcedure;
                        newcommand.Parameters.Add("@qno", System.Data.SqlDbType.Int).Value = ++entity.Qno;
                        newcommand.Parameters.Add("@userid", System.Data.SqlDbType.NVarChar).Value = this.User.Identity.Name;
                        await newcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                }
            }
            return string.Equals(answer, entity.Answer, System.StringComparison.InvariantCultureIgnoreCase);
        }

        [HttpGet]
        public async Task<IList<Models.EventLog>> GetLeaderboard(int qno, string ans)
        {
            IList<Models.EventLog> logs = new List<Models.EventLog>();
            using (SqlConnection connection = new SqlConnection(this.sqlConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using var sqlcommand = connection.CreateCommand();
                sqlcommand.CommandText = "GetLeaderboard";
                sqlcommand.CommandType = System.Data.CommandType.StoredProcedure;
                using(var reader = await sqlcommand.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (reader.HasRows)
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var log = new Models.EventLog();
                            log.UserId = reader.GetString(0);
                            log.Qno = reader.GetInt32(1);
                            logs.Add(log);
                        }
                    }
                }                
            }
            return logs;
        }
    }
}
