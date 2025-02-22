using Godot;
using System;
using OpenAI.Chat; // if you need any types from this namespace

public partial class EbookReader : Control
{
	// UI nodes
	private RichTextLabel pageLabel;
	private Button nextButton;
	private Button prevButton;
	
	// List to store paginated text and current page index
	private Godot.Collections.Array<string> pages = new Godot.Collections.Array<string>();
	private int currentPage = 0;
	private string content="";

	// Custom assistant API details
	private string assistantId = "asst_zh2w03jtgCokVqaca8otBDrR";
	private string apiKey = ""; // update with your API key

	// Endpoints
	private string createThreadAndRunEndpoint = "https://api.openai.com/v1/threads/runs";
	// When a thread already exists, use the "create message" endpoint:
	// POST https://api.openai.com/v1/threads/{thread_id}/messages

	// Store the thread id after the first run is created.
	private string currentThreadId = "";
	
	// HTTPRequest node for API calls
	private HttpRequest chatRequest;
	
	public override void _Ready()
	{
		// Get UI nodes
		pageLabel = GetNode<RichTextLabel>("PageLabel");
		nextButton = GetNode<Button>("NextButton");
		prevButton = GetNode<Button>("PrevButton");

		// Create and add an HTTPRequest node for API calls
		chatRequest = new HttpRequest();
		AddChild(chatRequest);
		chatRequest.Connect("request_completed", new Callable(this, "_OnChatGPTResponseCompleted"));

		// Load the text file and paginate it.
		string filePath = "res://books/Fahrenheit 451.txt";
		if (FileAccess.FileExists(filePath))
		{
			using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
			content = file.GetAsText();
			file.Close();

			// Split text into fixed-size pages (every 1000 characters)
			int pageLength = 1000;
			pages.Clear();
			for (int i = 0; i < content.Length; i += pageLength)
			{
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

	// Display the page and trigger a request with sentiment analysis instructions.
	private void ShowPage(int pageIndex)
	{
		if (pageIndex >= 0 && pageIndex < pages.Count)
		{
			pageLabel.Text = pages[pageIndex];
			// Append an instruction for sentiment analysis.
			string prompt = "json\n" + content + "\n\n" + pageLabel.Text;
			GetChatGPTResponse(prompt);
		}
	}

	private void OnNextButtonPressed()
	{
		if (currentPage < pages.Count - 1)
		{
			currentPage++;
			ShowPage(currentPage);
		}
	}

	private void OnPrevButtonPressed()
	{
		if (currentPage > 0)
		{
			currentPage--;
			ShowPage(currentPage);
		}
	}

	// Send a POST request:
	// - If no thread exists, create a new thread & run.
	// - Otherwise, add a message to the existing thread.
	private void GetChatGPTResponse(string prompt)
	{
		Godot.Collections.Dictionary payload;
		string url;

		if (string.IsNullOrEmpty(currentThreadId))
		{
			// No thread exists: create a new thread and run.
			url = createThreadAndRunEndpoint;
			payload = new Godot.Collections.Dictionary {
				{ "assistant_id", assistantId },
				{ "thread", new Godot.Collections.Dictionary {
					{ "messages", new Godot.Collections.Array {
						new Godot.Collections.Dictionary {
							{ "role", "user" },
							{ "content", prompt }
						}
					} }
				} },
				{ "temperature", 0.7 }
			};
		}
		else
		{
			// Thread already exists: add a new user message to the existing thread.
			url = "https://api.openai.com/v1/threads/" + currentThreadId + "/messages";
			payload = new Godot.Collections.Dictionary {
				{ "role", "user" },
				{ "content", prompt }
			};
		}

		string jsonPayload = Json.Stringify(payload);
		string[] headers = new string[] {
			"Content-Type: application/json",
			"Authorization: Bearer " + apiKey,
			"OpenAI-Beta: assistants=v2"
		};

		Error err = chatRequest.Request(url, headers, HttpClient.Method.Post, jsonPayload);
		if (err != Error.Ok)
		{
			GD.Print("Error sending request: " + err.ToString());
		}
	}

	// Callback when the HTTP request completes.
	private void _OnChatGPTResponseCompleted(int result, int responseCode, Godot.Collections.Array<string> headers, byte[] body)
	{
		string responseText = System.Text.Encoding.UTF8.GetString(body);
		GD.Print("[ASSISTANT]: " + responseText);

		// If this was the first request (run creation), extract and store the thread_id.
		if (string.IsNullOrEmpty(currentThreadId))
		{
			Json JSON= new Json();
			Error parseResult = JSON.Parse(responseText);
			if (parseResult == (int)Godot.Error.Ok)
			{
				var jsonObj = (Godot.Collections.Dictionary)JSON.Data;
				if (jsonObj.ContainsKey("thread_id"))
				{
					currentThreadId = (string)jsonObj["thread_id"];
					GD.Print("Stored thread_id: " + currentThreadId);
				}
			}
			else
			{
				GD.Print("JSON Parse error");
			}
		}
	}
}
