namespace PWebShop.Hybrid;

public partial class App : Application
{
	public App()
	{
        Console.WriteLine("MAUI_STARTUP: App constructor started");
		InitializeComponent();
        Console.WriteLine("MAUI_STARTUP: App InitializeComponent done");

		MainPage = new MainPage();
        Console.WriteLine("MAUI_STARTUP: App MainPage assigned");
	}
}
