﻿//-----------------------------------------------------------------------
// <copyright file="SolutionUploadDialog.xaml.cs" company="https://github.com/jhueppauff/Dynamics365-Customizing-Downloader">
// Copyright 2017 Jhueppauff
// MIT  
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

namespace Dynamics365CustomizingDownloader.Pages
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using Core.Xrm;

    /// <summary>
    /// Interaction logic for SolutionUpload
    /// </summary>
    public partial class SolutionUploadDialog : Window
    {
        /// <summary>
        /// BackGround Worker for the Import
        /// </summary>
        private readonly BackgroundWorker backgroundWorker = new BackgroundWorker();

        /// <summary>
        /// CRM Connection
        /// </summary>
        private CrmConnection crmConnection;

        /// <summary>
        /// <see cref="List{CrmSolution}"/> to Upload to CRM
        /// </summary>
        private List<CrmSolution> crmSolutions;

        /// <summary>
        /// Helper to iterate the Solution list
        /// </summary>
        private int uploadCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionUploadDialog"/> class.
        /// </summary>
        /// <param name="crmConnection">CRM Connection</param>
        /// <param name="crmSolutions">Solutions to Upload</param>
        public SolutionUploadDialog(CrmConnection crmConnection, List<CrmSolution> crmSolutions)
        {
            this.InitializeComponent();

            this.crmConnection = crmConnection;
            this.crmSolutions = crmSolutions;
        }

        /// <summary>
        /// Button Event, start Solution Import
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Btn_start_Click(object sender, RoutedEventArgs e)
        { 
            // Background Worker
            this.backgroundWorker.DoWork += this.Worker_DoWork;
            this.backgroundWorker.RunWorkerCompleted += this.Worker_RunWorkerCompleted;
            this.backgroundWorker.WorkerReportsProgress = true;

            foreach (CrmSolution crmSolution in this.crmSolutions)
            {
                this.backgroundWorker.RunWorkerAsync(crmSolution);
            }
        }

        /// <summary>
        /// Background Worker Event DoWork
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            CrmSolution crmSolution = (CrmSolution)e.Argument;

            ToolingConnector toolingConnector = new ToolingConnector();

            try
            {
                toolingConnector.UploadCrmSolution(crmSolution: crmSolution, crmServiceClient: toolingConnector.GetCrmServiceClient(this.crmConnection.ConnectionString));
            }
            catch (System.Exception ex)
            {
                tbx_status.Text += ("\n {0} \nStackTrace: {1}", ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        /// Background Worker Event Worker_RunWorkerCompleted
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Diagnostics.ErrorReport errorReport = new Diagnostics.ErrorReport(e.Error, "An error occured while downloading or extracting the solution");
                errorReport.Show();
            }
            else if (this.uploadCount == this.crmSolutions.Count)
            {
                tbx_status.Text += "\n Upload of {0} completed" + this.crmSolutions[this.uploadCount].LocalPath;
                tbx_status.Text += "\n Upload completed";
                pgr_upload.Value = 100;
            }
            else
            {
                pgr_upload.Value += (100 / uploadCount);
                tbx_status.Text += "\n Upload of {0} completed" + this.crmSolutions[this.uploadCount].LocalPath;
            }
        }
    }
}