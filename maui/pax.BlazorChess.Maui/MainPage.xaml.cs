namespace pax.BlazorChess.Maui
{
    public partial class MainPage : ContentPage
    {
        public MainPage(string startPath = "/")
        {
            InitializeComponent();
            blazorWebView.StartPath = startPath;
        }
    }
}
