using ClientBot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;


namespace ClientBot
{
    class Program
    {
        static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
            builder.Services.AddHttpClient();
            builder.Services.AddTransient<ApiService>();
            builder.Configuration.AddJsonFile("appsettings.json").AddEnvironmentVariables();


            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddUserSecrets<Program>();
            }

            var botToken = builder.Configuration["BotToken"] ?? throw new Exception("BotToken не найден");

            var apiService = builder.Services.BuildServiceProvider().GetService<ApiService>();

            var bot = new ClientBot(botToken, apiService);
            bot.StartReceiving();

                
        }   
    }
}