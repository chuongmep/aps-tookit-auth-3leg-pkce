using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace aps_tookit_auth_3leg_pkce
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static Random random = new Random();
		public MainWindow()
		{
			InitializeComponent();
		}

		private void LoginOnClick(object sender, RoutedEventArgs e)
		{
			string codeVerifier = RandomString(64);
			string codeChallenge = GenerateCodeChallenge(codeVerifier);
			Global.codeVerifier = codeVerifier;
			Global.ClientId = aps_tookit_auth_3leg_pkce.Resources.ClientId;
			Global.CallbackURL = aps_tookit_auth_3leg_pkce.Resources.CallbackUrl;
			redirectToLogin(codeChallenge);
			btnLogin.Content= "Proceed in the browser!";
		}

		private async void RefreshTokenOnClick(object sender, RoutedEventArgs e)
		{
			try
			{
				var client = new HttpClient();
				var request = new HttpRequestMessage
				{
					Method = HttpMethod.Post,
					RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v2/token"),
					Content = new FormUrlEncodedContent(new Dictionary<string, string>
					{
						{ "scope", "data:read" },
						{ "grant_type", "refresh_token" },
						{ "refresh_token", Global.RefreshToken },
						{ "client_id", Global.ClientId }
					}),
				};
				using (var response = await client.SendAsync(request))
				{
					response.EnsureSuccessStatusCode();
					string bodystring = await response.Content.ReadAsStringAsync();
					JObject bodyjson = JObject.Parse(bodystring);
					tbxToken.Text = Global.AccessToken = bodyjson["access_token"].Value<string>();
					Global.RefreshToken = bodyjson["refresh_token"].Value<string>();
				}
			}
			catch (Exception ex)
			{
				tbxToken.Text = "An error occurred!";
				lbnResult.Content = ex.Message;
			}
		}
		private void redirectToLogin(string codeChallenge)
		{
			string[] prefixes =
			{
				"http://localhost:8080/api/auth/"
			};
			System.Diagnostics.Process.Start($"https://developer.api.autodesk.com/authentication/v2/authorize?response_type=code&client_id={Global.ClientId}&redirect_uri={HttpUtility.UrlEncode(Global.CallbackURL)}&scope=data:read&prompt=login&code_challenge={codeChallenge}&code_challenge_method=S256");
			SimpleListenerExample(prefixes);
		}
		public async Task SimpleListenerExample(string[] prefixes)
		{
			if (!HttpListener.IsSupported)
			{
				throw new NotSupportedException("HttpListener is not supported in this context!");
			}
			// URI prefixes are required,
			// for example "http://contoso.com:8080/index/".
			if (prefixes == null || prefixes.Length == 0)
				throw new ArgumentException("prefixes");

			// Create a listener.
			HttpListener listener = new HttpListener();
			// Add the prefixes.
			foreach (string s in prefixes)
			{
				listener.Prefixes.Add(s);
			}
			listener.Start();
			//Console.WriteLine("Listening...");
			// Note: The GetContext method blocks while waiting for a request.
			HttpListenerContext context = listener.GetContext();
			HttpListenerRequest request = context.Request;
			// Obtain a response object.
			HttpListenerResponse response = context.Response;

			try
			{
				string authCode = request.Url.Query.ToString().Split('=')[1];
				await GetPKCEToken(authCode);
			}
			catch (Exception ex)
			{
				tbxToken.Text = "An error occurred!";
				lbnResult.Content= ex.Message;
			}

			// Construct a response.
			string responseString = "<HTML><BODY> You can move to the form!</BODY></HTML>";
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
			// Get a response stream and write the response to it.
			response.ContentLength64 = buffer.Length;
			System.IO.Stream output = response.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			// You must close the output stream.
			output.Close();
			listener.Stop();
		}

		public static string RandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
					.Select(s => s[random.Next(s.Length)]).ToArray());

			//Note: The use of the Random class makes this unsuitable for anything security related, such as creating passwords or tokens.Use the RNGCryptoServiceProvider class if you need a strong random number generator
		}

		private static string GenerateCodeChallenge(string codeVerifier)
		{
			var sha256 = SHA256.Create();
			var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
			var b64Hash = Convert.ToBase64String(hash);
			var code = Regex.Replace(b64Hash, "\\+", "-");
			code = Regex.Replace(code, "\\/", "_");
			code = Regex.Replace(code, "=+$", "");
			return code;
		}

		private async Task GetPKCEToken(string authCode)
		{
			try
			{
				var client = new HttpClient();
				var request = new HttpRequestMessage
				{
					Method = HttpMethod.Post,
					RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v2/token"),
					Content = new FormUrlEncodedContent(new Dictionary<string, string>
					{
							{ "client_id", Global.ClientId },
							{ "code_verifier", Global.codeVerifier },
							{ "code", authCode},
							{ "scope", "data:read" },
							{ "grant_type", "authorization_code" },
							{ "redirect_uri", Global.CallbackURL }
					}),
				};

				using (var response = await client.SendAsync(request))
				{
					response.EnsureSuccessStatusCode();
					string bodystring = await response.Content.ReadAsStringAsync();
					JObject bodyjson = JObject.Parse(bodystring);
					lbnResult.Content = "You can find your token below";
					tbxToken.Text = Global.AccessToken = bodyjson["access_token"].Value<string>();
					Global.RefreshToken = bodyjson["refresh_token"].Value<string>();
				}
			}
			catch (Exception ex)
			{
				tbxToken.Text = "An error occurred!";
				lbnResult.Content = ex.Message;
			}
		}

	}


}
