using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace SpotifyControl
{
    internal class Program
    {
        private static EmbedIOAuthServer _server;
        private static SpotifyClient spotify;

        private const int CALLBACK_PORT = 5543;
        private static TaskCompletionSource<bool> refresh_token_received;

        private const int VOLUME_BY = 10;  // percent

        private static readonly string CLIENT_ID = ConfigurationManager.AppSettings["CLIENT_ID"];
        private static readonly string CLIENT_SECRET = ConfigurationManager.AppSettings["CLIENT_SECRET"];

        private static string REFRESH_TOKEN = ConfigurationManager.AppSettings["REFRESH_TOKEN"];

        /*
         * Only one argument allowed: [r / + / - / 0]
         *  r : update the refresh token 
         *  + : increase volume 
         *  - : decrease volume
         *  0 : zero volume
         */

        public static async Task Main(string[] args)
        {
            if ((args.Length != 1) || ((args[0] != "-") && (args[0] != "+") && (args[0] != "0") && (args[0] != "r")))
            {
                Console.WriteLine("[ERROR] Bad arguments\n" +
                    "* Only one argument allowed: [r / + / - / 0]\n" +
                    "*  'SpotifyControl r' : update the refresh token\n" +
                    "*  'SpotifyControl +' : increase volume\n" +
                    "*  'SpotifyControl -' : decrease volume\n" +
                    "*  'SpotifyControl 0' : zero volume");
                return;
            }

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (args[0] == "r")
            {
                try
                {
                    await UpdateRefreshToken();
                    await refresh_token_received.Task;  // wait until refresh token received
                }
                catch
                {
                    Console.WriteLine("[ERROR] Can't get the refresh token (maybe any client id/secret issue?)");
                    return;
                }

                config.AppSettings.Settings["REFRESH_TOKEN"].Value = REFRESH_TOKEN;
                config.Save();

                Console.WriteLine("[SUCCESS] Received the refresh token successfully");
                return;
            }
            else if (((args[0] == "-") || (args[0] == "+") || (args[0] == "0")) && (REFRESH_TOKEN != null))
            {
                string accessToken;

                try
                {
                    accessToken = await GetAccessToken(REFRESH_TOKEN);
                }
                catch  // can't get the refresh token
                {
                    Console.WriteLine("[ERROR] Can't get the access token (issue with refresh token, refresh it via 'r' argument)");
                    return;
                }
                spotify = new SpotifyClient(accessToken);

                int currentVolume = await GetVolume();
                if (currentVolume == -1)
                {
                    Console.WriteLine("[ERROR] No device is active currently (does any song play?)");
                    return;
                }

                switch (args[0])
                {
                    case "+":
                        {
                            int newVolume = currentVolume + VOLUME_BY;
                            if (newVolume > 100)
                                newVolume = 100;

                            PlayerVolumeRequest volumeInfo = new(newVolume);
                            await spotify.Player.SetVolume(volumeInfo);
                            Console.WriteLine("[SUCCESS] + Volume");
                        }
                        break;

                    case "-":
                        {
                            int newVolume = currentVolume - VOLUME_BY;
                            if (newVolume < 0)
                                newVolume = 0;

                            PlayerVolumeRequest volumeInfo = new(newVolume);
                            await spotify.Player.SetVolume(volumeInfo);
                            Console.WriteLine("[SUCCESS] - Volume");
                        }
                        break;

                    case "0":
                        {
                            int newVolume = 0;

                            PlayerVolumeRequest volumeInfo = new(newVolume);
                            await spotify.Player.SetVolume(volumeInfo);
                            Console.WriteLine("[SUCCESS] 0 Volume");
                        }
                        break;
                }
            }
        }

        private static async Task<string> GetAccessToken(string refreshToken)
        {
            AuthorizationCodeRefreshResponse newResponse = await new OAuthClient().RequestToken(new AuthorizationCodeRefreshRequest(CLIENT_ID, CLIENT_SECRET, refreshToken));
            return newResponse.AccessToken;
        }

        // Updates REFRESH_TOKEN field
        private static async Task UpdateRefreshToken()
        {
            await LoginSpotify();
            refresh_token_received = new TaskCompletionSource<bool>();  // automatically task set to false (not already done) in order to make main thread to wait until refresh token will be received
        }

        private static async Task LoginSpotify()
        {
            _server = new EmbedIOAuthServer(new Uri($"http://localhost:{CALLBACK_PORT}/callback"), CALLBACK_PORT);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            LoginRequest request = new(_server.BaseUri, CLIENT_ID, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { Scopes.UserReadPlaybackState, Scopes.UserModifyPlaybackState }
            };

            BrowserUtil.Open(request.ToUri());
        }

        private static async Task<int> GetVolume()
        {
            DeviceResponse devices = await spotify.Player.GetAvailableDevices();
            foreach (Device device in devices.Devices)
            {
                if (device.IsActive)
                {
                    return (int)device.VolumePercent;
                }
            }
            return -1;
        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();

            SpotifyClientConfig config = SpotifyClientConfig.CreateDefault();
            AuthorizationCodeTokenResponse tokenResponse = await new OAuthClient(config).RequestToken(
              new AuthorizationCodeTokenRequest(
                CLIENT_ID, CLIENT_SECRET, response.Code, new Uri($"http://localhost:{CALLBACK_PORT}/callback")
              )
            );

            REFRESH_TOKEN = tokenResponse.RefreshToken;
            refresh_token_received.SetResult(true);
        }

        private static async Task OnErrorReceived(object sender, string error, string state)
        {
            await _server.Stop();
        }
    }
}
