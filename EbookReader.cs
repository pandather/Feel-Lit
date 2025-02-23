using Godot;
using OpenAI;
using OpenAI.Assistants;

public partial class EbookReader : Control
{
	#pragma warning disable OPENAI001
	// UI nodes
	private RichTextLabel pageLabel;
	private Button nextButton;
	private Button prevButton;

	// List of paginated pages + current index
	private Godot.Collections.Array<string> pages = new Godot.Collections.Array<string>();
	private int currentPage;

	// ========== OPENAI ASSISTANT SETUP ==========

	private string apiKey = "sk-proj-..."; // truncated for brevity
	private string assistantId = "asst_zh2w03jtgCokVqaca8otBDrR"; // Must exist in your account

	// We'll create these in _Ready()
	private OpenAIClient openAIClient;
	private AssistantClient assistantClient;
	private Assistant assistant;

	// Track the current thread and run
	private string currentThreadId = "";
	private ThreadRun currentRun;

	// Timer to poll run status
	private Timer pollTimer;

	public override void _Ready()
	{
		// ========== 1) Grab UI nodes ==========
		pageLabel = GetNode<RichTextLabel>("PageLabel");
		nextButton = GetNode<Button>("NextButton");
		prevButton = GetNode<Button>("PrevButton");

		nextButton.Pressed += OnNextButtonPressed;
		prevButton.Pressed += OnPrevButtonPressed;

		// ========== 2) Initialize the OpenAI library ==========
		openAIClient = new OpenAIClient(apiKey);
		assistantClient = openAIClient.GetAssistantClient();

		// Attempt to get an existing Assistant by ID:
		bool gotAssistant = true;
		try
		{
			assistant = assistantClient.GetAssistant(assistantId);
		}
		catch
		{
			gotAssistant = false;
		}

		if (!gotAssistant || assistant == null)
		{
			GD.PrintErr($"Could not find assistant with ID = {assistantId}.");
			// If needed, create one here...
			// assistant = assistantClient.CreateAssistant(...);
		}
		else
		{
			GD.Print($"Using Assistant: {assistant.Id}");
		}

		// ========== 3) Load & paginate text file ==========
		string filePath = "res://books/Fahrenheit 451.txt";
		if (!FileAccess.FileExists(filePath))
		{
			pageLabel.Text = "File not found!";
			return;
		}

		// Read the entire text
		var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
		var fullText = file.GetAsText();
		file.Close();

		// Paginate in chunks (example: 1000 chars)
		int pageChunkSize = 1000;
		int position = 0;
		while (position < fullText.Length)
		{
			int length = pageChunkSize;
			if (position + pageChunkSize > fullText.Length)
				length = fullText.Length - position;

			var pageText = fullText.Substr(position, length);
			pages.Add(pageText);
			position += pageChunkSize;
		}

		// ========== 4) Timer to poll run status ==========
		pollTimer = new Timer();
		pollTimer.OneShot = false;
		pollTimer.WaitTime = 1.0f; // poll once per second
		pollTimer.Timeout += CheckRunStatus;
		AddChild(pollTimer);

		// Show the first page
		currentPage = 0;
		DisplayPage(currentPage);
	}

	private void OnNextButtonPressed()
	{
		if (currentPage < pages.Count - 1)
		{
			currentPage++;
			DisplayPage(currentPage);
		}
	}

	private void OnPrevButtonPressed()
	{
		if (currentPage > 0)
		{
			currentPage--;
			DisplayPage(currentPage);
		}
	}

	private void DisplayPage(int index)
	{
		if (index < 0 || index >= pages.Count)
			return;

		pageLabel.Text = pages[index];
		SendPageForAnalysis(pages[index]);
	}

	// --------------------------------------------------------------------------------
	//  Send the new page text to the Assistant
	// --------------------------------------------------------------------------------
	private void SendPageForAnalysis(string pageText)
	{
		if (assistant == null)
		{
			GD.PrintErr("No valid Assistant configured.");
			return;
		}

		// We'll ask for a quick sentiment analysis
		string userMessage = "Please perform a brief sentiment analysis on this text:\n" + pageText;

		// If no thread yet, create one:
		if (string.IsNullOrEmpty(currentThreadId))
		{
			CreateThreadAndRun(userMessage);
		}
		else
		{
			CreateRunOnExistingThread(userMessage);
		}

		// Start polling for run completion
		pollTimer.Start();
	}

	private void CreateThreadAndRun(string userMessage)
	{
		GD.Print("Creating new thread + run...");

		// You can pass only initial messages in ThreadCreationOptions
		var threadOptions = new ThreadCreationOptions
		{
			InitialMessages = { userMessage }
		};

		// Then specify model, temperature, etc. in RunCreationOptions
		var runOptions = new RunCreationOptions
		{
			Parameters = new ChatCompletionParameters
			{
				Model = "gpt-4o",     // or whichever model your assistant is set to use
				Temperature = 0.7f
			}
		};

		// Create both thread and run in one call
		currentRun = assistantClient.CreateThreadAndRun(assistant.Id, threadOptions, runOptions);

		currentThreadId = currentRun.ThreadId;
		GD.Print($"[CreateThreadAndRun] new thread_id={currentThreadId}, run_id={currentRun.Id}");
	}

	private void CreateRunOnExistingThread(string userMessage)
	{
		GD.Print("Creating run on existing thread...");

		// For subsequent runs in the same thread, use AdditionalMessages
		var runOptions = new RunCreationOptions
		{
			AdditionalMessages = { userMessage },
			Parameters = new ChatCompletionParameters
			{
				Model = "gpt-4o",
				Temperature = 0.7f
			}
		};

		currentRun = assistantClient.CreateRun(currentThreadId, assistant.Id, runOptions);
		GD.Print($"[CreateRun] run_id={currentRun.Id}");
	}

	// --------------------------------------------------------------------------------
	// Poll for run completion once per second in CheckRunStatus()
	// --------------------------------------------------------------------------------
	private void CheckRunStatus()
	{
		if (currentRun == null || currentRun.Status.IsTerminal)
		{
			pollTimer.Stop();
			return;
		}

		// Refresh the run status
		currentRun = assistantClient.GetRun(currentRun.ThreadId, currentRun.Id);
		GD.Print("Run status: ", currentRun.Status);

		if (currentRun.Status.IsTerminal)
		{
			pollTimer.Stop();
			FetchAndPrintAssistantReply();
		}
	}

	private void FetchAndPrintAssistantReply()
	{
		if (string.IsNullOrEmpty(currentThreadId))
			return;

		var options = new MessageCollectionOptions
		{
			Order = MessageCollectionOrder.Ascending
		};

		// The returned collection now commonly has an `.Items` property
		var messages = assistantClient.GetMessages(currentThreadId, options);

		GD.Print("===== FULL CONVERSATION =====");
		foreach (var msg in messages.Items)
		{
			GD.Print("[ROLE: ", msg.Role, "]");
			foreach (var block in msg.Content)
			{
				if (!string.IsNullOrEmpty(block.Text))
				{
					GD.Print("  ", block.Text);
				}
			}
		}
		GD.Print("===== END CONVERSATION =====");

		// Optionally locate the newest assistant message
		ThreadMessage newestAssistantMsg = null;
		for (int i = messages.Items.Count - 1; i >= 0; i--)
		{
			if (messages.Items[i].Role == MessageRole.Assistant)
			{
				newestAssistantMsg = messages.Items[i];
				break;
			}
		}

		if (newestAssistantMsg != null)
		{
			// Combine all text blocks
			var assistantReply = "";
			foreach (var block in newestAssistantMsg.Content)
			{
				if (!string.IsNullOrEmpty(block.Text))
				{
					assistantReply += block.Text + "\n";
				}
			}
			GD.Print("[Newest Assistant Reply]:\n", assistantReply);
		}
		else
		{
			GD.Print("No assistant message found yet.");
		}
	}
	#pragma warning restore OPENAI001
}
