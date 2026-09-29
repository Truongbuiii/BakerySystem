using Microsoft.UI.Xaml;
using System.IO;

namespace BakerySystem.MobileApp.WinUI;

public partial class App : MauiWinUIApplication
{
	public App()
	{
		this.UnhandledException += (sender, args) =>
		{
			File.WriteAllText("D:\\UNIVERSITY\\CNPM\\BakerySystem\\crash.log", args.Exception?.ToString() ?? "Unknown exception");
		};

		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
