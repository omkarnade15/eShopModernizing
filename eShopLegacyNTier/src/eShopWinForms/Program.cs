using eShopWinForms.Controllers;
using eShopWinForms.eShopServiceReference;
using System;
using System.ServiceModel;
using System.Windows.Forms;

namespace eShopWinForms
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CatalogView catalogView = new CatalogView();

            // Configure WCF client with explicit binding and endpoint address
            var binding = new BasicHttpBinding();
            var endpointAddress = new EndpointAddress("http://localhost:62314/CatalogService.svc");
            ICatalogService service = new eShopServiceReference.CatalogServiceClient(binding, endpointAddress);

            CatalogController catalogController = new CatalogController(service, catalogView);

            catalogController.LoadView();
            catalogView.ShowDialog();
        }
    }
}
