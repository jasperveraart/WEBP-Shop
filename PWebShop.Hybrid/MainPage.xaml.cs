namespace PWebShop.Hybrid;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
        Console.WriteLine("MAUI_STARTUP: MainPage constructor started");
		InitializeComponent();
        Console.WriteLine("MAUI_STARTUP: MainPage InitializeComponent done");
	}
}
