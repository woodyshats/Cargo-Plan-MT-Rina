using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace CargoPlanApp.Views
{
    public partial class PrintPreviewWindow : Window
    {
        private FlowDocument _document;

        public PrintPreviewWindow(FlowDocument document)
        {
            InitializeComponent();
            _document = document;
            DocReader.Document = _document;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Печатаем FlowDocument (автоматическое разбиение на страницы)
                IDocumentPaginatorSource idpSource = _document;
                printDialog.PrintDocument(idpSource.DocumentPaginator, "Cargo Plan");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}