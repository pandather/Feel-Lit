using Godot;
using Godot.Collections;
using Datafeel;
using Datafeel.NET.Serial;

public partial class DatafeelController : Node
{
	private DotManager manager;
	private Array dots = new Array(); // Using Godot.Collections.Array for dot storage.
	
	// Timer for periodic updates.
	private Timer updateTimer;

	public override void _Ready()
	{
		// Configure the DotManager with four dots.
		manager = new DotManagerConfiguration()
					.AddDot<Dot_63x_xxx>(1)
					.AddDot<Dot_63x_xxx>(2)
					.AddDot<Dot_63x_xxx>(3)
					.AddDot<Dot_63x_xxx>(4)
					.CreateDotManager();

		// Populate the dots array.
		dots.Add((Godot.Variant)(object)new DotPropsWritable()
		{
			Address = 1,
			LedMode = LedModes.Breathe,
			GlobalLed = new(),
			VibrationMode = VibrationModes.Manual,
			VibrationIntensity = 1.0f,
			VibrationFrequency = 170
		});
		dots.Add((Godot.Variant)(object)new DotPropsWritable()
		{
			Address = 2,
			LedMode = LedModes.Breathe,
			GlobalLed = new(),
			VibrationMode = VibrationModes.Manual,
			VibrationIntensity = 1.0f,
			VibrationFrequency = 170
		});
		dots.Add((Godot.Variant)(object)new DotPropsWritable()
		{
			Address = 3,
			LedMode = LedModes.Breathe,
			GlobalLed = new(),
			VibrationMode = VibrationModes.Manual,
			VibrationIntensity = 1.0f,
			VibrationFrequency = 170
		});
		dots.Add((Godot.Variant)(object)new DotPropsWritable()
		{
			Address = 4,
			LedMode = LedModes.Breathe,
			GlobalLed = new(),
			VibrationMode = VibrationModes.Manual,
			VibrationIntensity = 1.0f,
			VibrationFrequency = 170
		});

		// Create a Timer node to start the DotManager after 1 second.
		Timer startTimer = new Timer();
		startTimer.WaitTime = 1.0f;
		startTimer.OneShot = true;
		AddChild(startTimer);
		startTimer.Connect("timeout", new Callable(this, "OnStartTimerTimeout"));
		startTimer.Start();

		// Create a Timer node for periodic updates every 0.1 seconds.
		updateTimer = new Timer();
		updateTimer.WaitTime = 0.1f;
		updateTimer.OneShot = false;
		updateTimer.Autostart = true;
		AddChild(updateTimer);
		updateTimer.Connect("timeout", new Callable(this, "OnUpdateTimerTimeout"));
	}

	// Mark the method async so we can await the Start method.
	private async void OnStartTimerTimeout()
	{
		// Create the serial client.
		var serialClient = new DatafeelModbusClientConfiguration()
								.UseWindowsSerialPortTransceiver()
								//.UseSerialPort("COM3") // Uncomment this line to specify the serial port by name.
								.CreateClient();

		// Create a System.Collections.Generic.List for clients.
		var clients = new global::System.Collections.Generic.List<DatafeelModbusClient>();
		clients.Add(serialClient);

		// Await the asynchronous Start call.
		bool result = await manager.Start(clients, default(global::System.Threading.CancellationToken));
		if (result)
		{
			GD.Print("Datafeel Manager started successfully.");
		}
		else
		{
			GD.Print("Failed to start Datafeel Manager.");
		}
	}

	// Called every 0.1 seconds by the update timer.
	private void OnUpdateTimerTimeout()
	{
		// Loop through each dot, update its properties, then write and read them.
		for (int i = 0; i < dots.Count; i++)
		{
			// Cast each element from Variant to DotPropsWritable.
			DotPropsWritable d = (DotPropsWritable)(object)dots[i];

			d.VibrationIntensity = 1.0f;
			d.VibrationFrequency += 10;
			if (d.VibrationFrequency > 250)
			{
				d.VibrationFrequency = 100;
			}
			manager.Write(d);
			var readResult = manager.Read(d);
		}
	}
}
