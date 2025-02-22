using Godot;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;
using OpenAI.Chat;

public partial class EbookReader : Control
{
	private HttpClient httpClient;
	
	// UI nodes
	private RichTextLabel pageLabel;
	private Button nextButton;
	private Button prevButton;

	// List to store paginated text and current page index
	private Godot.Collections.Array<string> pages = new Godot.Collections.Array<string>();
	private int currentPage = 0;
	private static readonly string apiKey = "api_key";
	private static readonly string endpoint = "https://api.openai.com/v1/chat/completions";


	public override void _Ready()
	{
		// Get UI nodes
		pageLabel = GetNode<RichTextLabel>("PageLabel");
		nextButton = GetNode<Button>("NextButton");
		prevButton = GetNode<Button>("PrevButton");

		httpClient = new HttpClient();

		// Load the text file
		GD.Print("content");
		string filePath = "res://books/Fahrenheit 451.txt";
		if (FileAccess.FileExists(filePath))
			GD.Print("file exists");

		if (FileAccess.FileExists(filePath))
		{
			using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
			string content = file.GetAsText();
			file.Close();

			// Fixed-size pagination: split text every 1000 characters.
			int pageLength = 1000;
			pages.Clear();
			for (int i = 0; i < content.Length; i += pageLength)
			{
				// If remaining text is less than a full page, take the rest
				if (i + pageLength > content.Length)
					pages.Add(content.Substring(i));
				else
					pages.Add(content.Substring(i, pageLength));
			}
			currentPage = 0;
			ShowPage(currentPage);
		}
		else
		{
			pageLabel.Text = "File not found!";
		}

		// Connect button signals
		nextButton.Pressed += OnNextButtonPressed;
		prevButton.Pressed += OnPrevButtonPressed;
	}

	// Display the page at the given index
	async private void ShowPage(int pageIndex)
	{
		if (pageIndex >= 0 && pageIndex < pages.Count)
		{
			pageLabel.Text = pages[pageIndex];
			GD.Print(pageLabel.Text);
			string response = GetChatGPTResponse(pageLabel.Text+"\n\n Use sentiment analysis to determine an affect for each page of an ebook");
			GD.Print(response);
		}
	}

	// Signal handler for the Next button
	private void OnNextButtonPressed()
	{
		if (currentPage < pages.Count - 1)
		{
			currentPage++;
			ShowPage(currentPage);
		}
	}

	// Signal handler for the Previous button
	private void OnPrevButtonPressed()
	{
		if (currentPage > 0)
		{
			currentPage--;
			ShowPage(currentPage);
		}
	}

	//// Call OpenAI API using Godot.HttpClient
	//private async Task<string> GetChatGPTResponse(string prompt)
	//{
		//var jsonRequest = "{\"model\":\"gpt-4\",\"messages\":[{\"role\":\"system\",\"content\":\"You are a helpful assistant.\"},{\"role\":\"user\",\"content\":\"" + prompt + "\"}],\"max_tokens\":100}";
		//var request = new HttpRequest();
		//AddChild(request);
//
		//request.RequestCompleted += (long result, long responseCode, string[] headers, byte[] body) =>
		//{
			//if (responseCode == 200)
			//{
				//string response = Encoding.UTF8.GetString(body);
				//GD.Print("Response: " + response);
			//}
			//else
			//{
				//GD.PrintErr("Error: " + responseCode);
			//}
		//};
//
		//var headers = new string[]
		//{
			//"Authorization: Bearer " + apiKey,
			//"Content-Type: application/json"
		//};
		//request.Request(endpoint, headers, HttpClient.Method.Post, jsonRequest);
		//return "";
	//}
	// Call OpenAI API using Godot.HttpClient
private string GetChatGPTResponse(string prompt)
{
	//ChatClient client = new(model: "gpt-4o", apiKey: Environment.GetEnvironmentVariable(apiKey));
//
	//ChatCompletion completion = client.CompleteChat("Say 'this is a test.'");
//
	//GD.Print($"[ASSISTANT]: {completion.Content[0].Text}");
	return prompt;
}
}
