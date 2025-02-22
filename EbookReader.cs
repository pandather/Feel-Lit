using Godot;
using System;
using System.Collections.Generic;
//using OpenAI.Chat;
//using OpenAI;

public partial class EbookReader : Control
{
	// UI nodes
	private RichTextLabel pageLabel;
	private Button nextButton;
	private Button prevButton;

	// List to store paginated text and current page index
	private List<string> pages = new List<string>();
	private int currentPage = 0;

	public override void _Ready()
	{
		//var builder = new ConfigurationBuilder()
			//.SetBasePath(Directory.GetCurrentDirectory())
			//.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
//
		//IConfiguration config = builder.Build();
//
		//string apiKey = config["OpenAI:ApiKey"] ?? 
			//throw new InvalidOperationException("API key not found in configuration.");
//
		//ChatClient client = new(model: "gpt-4", apiKey: apiKey);
		// Get UI nodes
		pageLabel = GetNode<RichTextLabel>("PageLabel");
		nextButton = GetNode<Button>("NextButton");
		prevButton = GetNode<Button>("PrevButton");

		// Load the text file
		GD.Print("content");
		string filePath = "res://books/Fahrenheit 451.txt";
		if(FileAccess.FileExists(filePath))
			GD.Print("file exists");
		// using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
		// string content = file.GetAsText();
		// //GD.Print(content);
	//return content;
//
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
		// nextButton.Connect("pressed", this, nameof(OnNextButtonPressed));
		nextButton.Pressed += OnNextButtonPressed;
		prevButton.Pressed += OnPrevButtonPressed;
		// prevButton.Connect("pressed", this, nameof(OnPrevButtonPressed));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	// Display the page at the given index
	private void ShowPage(int pageIndex)
	{
		if (pageIndex >= 0 && pageIndex < pages.Count)
		{
			pageLabel.Text = pages[pageIndex];
			GD.Print(pageLabel.Text);
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
}
