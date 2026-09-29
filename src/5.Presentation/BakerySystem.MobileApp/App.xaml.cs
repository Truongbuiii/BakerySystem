namespace BakerySystem.MobileApp;

public partial class App : Microsoft.Maui.Controls.Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage()) 
		{ 
			Title = "Bakery Mobile - Tiệm Bánh Thủ Công" 
		};

		window.Created += (s, e) =>
		{
			try
			{
				window.Width = 430;
				window.Height = 860;
			}
			catch { }
		};

		return window;
	}
}
