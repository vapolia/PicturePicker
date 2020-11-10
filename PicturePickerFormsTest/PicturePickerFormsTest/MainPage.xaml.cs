using Xamarin.Forms;

namespace PicturePickerFormsTest
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainPageViewModel(this);
        }
    }
}
