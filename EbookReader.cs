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

	// Custom assistant API details
	private string assistantId = "asst_zh2w03jtgCokVqaca8otBDrR";
	private string apiKey = "";

	// Endpoints
	private string createThreadAndRunEndpoint = "https://api.openai.com/v1/threads/runs";
	// For adding subsequent messages:
	// POST https://api.openai.com/v1/threads/{thread_id}/messages
	// For retrieving messages:
	// GET https://api.openai.com/v1/threads/{thread_id}/messages

	// Stored thread id.
	private string currentThreadId = "";
	
	// HTTPRequest nodes for API calls.
	private HttpRequest chatRequest;
	private HttpRequest messageListRequest; // For polling assistant responses.

	// Timer for polling.
	private Timer pollTimer;

	public override void _Ready()
	{
		// Get UI nodes.
		pageLabel = GetNode<RichTextLabel>("PageLabel");
		nextButton = GetNode<Button>("NextButton");
		prevButton = GetNode<Button>("PrevButton");

		// Create HTTPRequest nodes.
		chatRequest = new HttpRequest();
		AddChild(chatRequest);
		chatRequest.Connect("request_completed", new Callable(this, "_OnChatGPTResponseCompleted"));

		messageListRequest = new HttpRequest();
		AddChild(messageListRequest);
		messageListRequest.Connect("request_completed", new Callable(this, "_OnMessageListResponseCompleted"));

		// Create a Timer for polling.
		pollTimer = new Timer();
		pollTimer.WaitTime = 20.0f; // poll every 2 seconds (adjust as needed)
		pollTimer.OneShot = false;
		AddChild(pollTimer);
		pollTimer.Connect("timeout", new Callable(this, "PollForMessages"));

		// Load the text file and paginate.
		string filePath = "res://books/Fahrenheit 451.txt";
		if (FileAccess.FileExists(filePath))
		{
			using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
			string content = file.GetAsText();
			file.Close();

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

		nextButton.Pressed += OnNextButtonPressed;
		prevButton.Pressed += OnPrevButtonPressed;
	}

	// Show page and send request.
	private void ShowPage(int pageIndex)
	{
		if (pageIndex >= 0 && pageIndex < pages.Count)
		{
			pageLabel.Text = pages[pageIndex];
			string prompt = "json\n" + pageLabel.Text + "\n\nUse sentiment analysis to determine the affect for this page of an ebook.";
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

	// Sends a POST request:
	// - If no thread exists, create a new thread and run.
	// - Otherwise, add a message to the existing thread.
	private void GetChatGPTResponse(string prompt)
	{
		Godot.Collections.Dictionary payload;
		string url;

		if (string.IsNullOrEmpty(currentThreadId))
		{
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
			// Add message to the existing thread.
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

	// Callback for POST responses.
	private void _OnChatGPTResponseCompleted(int result, int responseCode, Godot.Collections.Array<string> headers, byte[] body)
	{
		string responseText = System.Text.Encoding.UTF8.GetString(body);
		GD.Print("[CHAT RESPONSE]: " + responseText);

		// If this is the first run, store the thread id.
		if (string.IsNullOrEmpty(currentThreadId))
		{
			Json JSON=new Json();
			var parseResult = JSON.Parse(responseText);
			if (parseResult == (int)Godot.Error.Ok)
			{
				var jsonObj = (Godot.Collections.Dictionary)JSON.Data;
				if (jsonObj.ContainsKey("thread_id"))
				{
					currentThreadId = (string)jsonObj["thread_id"];
					GD.Print("Stored thread_id: " + currentThreadId);
					// Start polling for new messages.
					pollTimer.Start();
				}
			}
			else
			{
				GD.Print("JSON Parse error in chat response");
			}
		}
	}

	// Poll for messages from the thread.
	private void PollForMessages()
	{
		if (!string.IsNullOrEmpty(currentThreadId))
		{
			string url = "https://api.openai.com/v1/threads/" + currentThreadId + "/messages";
			string[] headers = new string[] {
				"Content-Type: application/json",
				"Authorization: Bearer " + apiKey,
				"OpenAI-Beta: assistants=v2"
			};
			Error err = messageListRequest.Request(url, headers, HttpClient.Method.Get);
			if (err != Error.Ok)
			{
				GD.Print("Error polling messages: " + err.ToString());
			}
		}
	}

	// Callback for GET messages response.
	private void _OnMessageListResponseCompleted(int result, int responseCode, Godot.Collections.Array<string> headers, byte[] body)
	{
		string responseText = System.Text.Encoding.UTF8.GetString(body);
		GD.Print("[MESSAGE LIST]: " + responseText);

			Json JSON=new Json();
			var parseResult = JSON.Parse(responseText);
			if (parseResult == (int)Godot.Error.Ok)
		{
			var jsonObj = (Godot.Collections.Dictionary)JSON.Data;
			// Assuming the response object contains a "data" key with an array of message objects.
			if (jsonObj.ContainsKey("data"))
			{
				Godot.Collections.Array messages = (Godot.Collections.Array)jsonObj["data"];
				// Iterate over messages to find the latest assistant message.
				foreach (Godot.Collections.Dictionary msg in messages)
				{
					if (msg.ContainsKey("role") && (string)msg["role"] == "assistant")
					{
						// Here you can extract and display the assistant's response.
						if (msg.ContainsKey("content"))
						{
							Godot.Collections.Array contentArray = (Godot.Collections.Array)msg["content"];
							// For simplicity, assume the first element contains the text.
							if (contentArray.Count > 0)
							{
								Godot.Collections.Dictionary textDict = (Godot.Collections.Dictionary)contentArray[0];
								if (textDict.ContainsKey("text"))
								{
									Godot.Collections.Dictionary textContent = (Godot.Collections.Dictionary)textDict["text"];
									if (textContent.ContainsKey("value"))
									{
										string assistantReply = (string)textContent["value"];
										GD.Print("Assistant reply: " + assistantReply);
										// Update your UI or process the reply as needed.
									}
								}
							}
						}
					}
				}
			}
		}
		else
		{
			GD.Print("JSON Parse error in message list response");
		}
	}
}
