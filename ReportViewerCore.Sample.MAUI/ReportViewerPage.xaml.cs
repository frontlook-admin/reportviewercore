using Microsoft.Reporting.NETCore;
// Aliased to avoid ambiguity with Microsoft.Reporting.NETCore types
using MauiModels = Microsoft.ReportViewer.MAUI.Models;

namespace ReportViewerCore.Sample.MAUI;

[QueryProperty(nameof(ReportName), "ReportName")]
public partial class ReportViewerPage : ContentPage
{
    private string _reportName = string.Empty;

    public string ReportName
    {
        get => _reportName;
        set
        {
            _reportName = value;
            ReportTitle = GetReportTitle(value);
            OnPropertyChanged(nameof(ReportTitle));
            LoadReportAsync(value);
        }
    }

    public string ReportTitle { get; private set; } = "Report Viewer";

    public ReportViewerPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private static string GetReportTitle(string reportName) => reportName switch
    {
        "SimpleReport" => "Simple Report",
        "TrialBalanceReport" => "Trial Balance",
        "DataDrivenReport" => "Data-Driven Report",
        _ => "Report"
    };

    private async void LoadReportAsync(string reportName)
    {
        loadingOverlay.IsVisible = true;

        try
        {
            await Task.Delay(100); // Allow UI to update

            switch (reportName)
            {
                case "SimpleReport":
                    await LoadSimpleReportAsync();
                    break;
                case "TrialBalanceReport":
                    await LoadTrialBalanceReportAsync();
                    break;
                case "DataDrivenReport":
                    await LoadDataDrivenReportAsync();
                    break;
                default:
                    await DisplayAlert("Error", $"Unknown report: {reportName}", "OK");
                    break;
            }
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException != null ? $"\nInner: {ex.InnerException.Message}" : "";
            var fullMsg = $"Failed to load report: {ex.Message}{innerMsg}\n\nType: {ex.GetType().Name}";
            await DisplayAlert("Error", fullMsg, "OK");
        }
        finally
        {
            loadingOverlay.IsVisible = false;
        }
    }

    private async Task LoadSimpleReportAsync()
    {
        // Create a simple inline report
        var reportDefinition = CreateSimpleReportDefinition();
        
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reportDefinition));
        await reportViewer.LoadReportFromStreamAsync(stream);
        
        // Add sample data
        var items = new[]
        {
            new { Name = "Item 1", Quantity = 10, Price = 25.00m },
            new { Name = "Item 2", Quantity = 5, Price = 50.00m },
            new { Name = "Item 3", Quantity = 20, Price = 15.00m },
            new { Name = "Item 4", Quantity = 8, Price = 75.00m },
            new { Name = "Item 5", Quantity = 12, Price = 30.00m }
        };
        
        reportViewer.AddDataSource("Items", items);
        
        // Set parameters
        reportViewer.SetParameters(new[]
        {
            new ReportParameter("ReportTitle", "Sample Inventory Report"),
            new ReportParameter("GeneratedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm"))
        });
        
        await reportViewer.RefreshReportAsync();
    }

    private async Task LoadTrialBalanceReportAsync()
    {
        // Create trial balance report definition
        var reportDefinition = CreateTrialBalanceReportDefinition();
        
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reportDefinition));
        await reportViewer.LoadReportFromStreamAsync(stream);
        
        // Sample trial balance data
        var accounts = new[]
        {
            new { AccountCode = "1000", AccountName = "Cash", Debit = 50000.00m, Credit = 0.00m },
            new { AccountCode = "1100", AccountName = "Accounts Receivable", Debit = 25000.00m, Credit = 0.00m },
            new { AccountCode = "1200", AccountName = "Inventory", Debit = 75000.00m, Credit = 0.00m },
            new { AccountCode = "2000", AccountName = "Accounts Payable", Debit = 0.00m, Credit = 30000.00m },
            new { AccountCode = "3000", AccountName = "Common Stock", Debit = 0.00m, Credit = 100000.00m },
            new { AccountCode = "4000", AccountName = "Revenue", Debit = 0.00m, Credit = 80000.00m },
            new { AccountCode = "5000", AccountName = "Cost of Goods Sold", Debit = 40000.00m, Credit = 0.00m },
            new { AccountCode = "6000", AccountName = "Operating Expenses", Debit = 20000.00m, Credit = 0.00m }
        };
        
        reportViewer.AddDataSource("Accounts", accounts);
        
        reportViewer.SetParameters(new[]
        {
            new ReportParameter("CompanyName", "Sample Corporation"),
            new ReportParameter("ReportDate", DateTime.Now.ToString("MMMM dd, yyyy"))
        });
        
        await reportViewer.RefreshReportAsync();
    }

    private async Task LoadDataDrivenReportAsync()
    {
        var reportDefinition = CreateDataDrivenReportDefinition();
        
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reportDefinition));
        await reportViewer.LoadReportFromStreamAsync(stream);
        
        // Sample employee data
        var employees = new[]
        {
            new { EmployeeId = 1, Name = "John Smith", Department = "Engineering", Salary = 85000.00m, HireDate = new DateTime(2020, 3, 15) },
            new { EmployeeId = 2, Name = "Jane Doe", Department = "Marketing", Salary = 72000.00m, HireDate = new DateTime(2019, 7, 1) },
            new { EmployeeId = 3, Name = "Bob Johnson", Department = "Engineering", Salary = 92000.00m, HireDate = new DateTime(2018, 11, 20) },
            new { EmployeeId = 4, Name = "Alice Brown", Department = "HR", Salary = 65000.00m, HireDate = new DateTime(2021, 1, 10) },
            new { EmployeeId = 5, Name = "Charlie Wilson", Department = "Engineering", Salary = 78000.00m, HireDate = new DateTime(2022, 5, 25) },
            new { EmployeeId = 6, Name = "Diana Miller", Department = "Marketing", Salary = 68000.00m, HireDate = new DateTime(2021, 8, 14) }
        };
        
        reportViewer.AddDataSource("Employees", employees);
        
        reportViewer.SetParameters(new[]
        {
            new ReportParameter("ReportTitle", "Employee Directory"),
            new ReportParameter("GeneratedBy", "Report System")
        });
        
        await reportViewer.RefreshReportAsync();
    }

    #region Report Definitions

    private static string CreateSimpleReportDefinition()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <AutoRefresh>0</AutoRefresh>
  <ReportParameters>
    <ReportParameter Name=""ReportTitle""><DataType>String</DataType><Prompt>Report Title</Prompt></ReportParameter>
    <ReportParameter Name=""GeneratedDate""><DataType>String</DataType><Prompt>Generated Date</Prompt></ReportParameter>
  </ReportParameters>
  <DataSources>
    <DataSource Name=""DataSource1""><ConnectionProperties><DataProvider>System.Data.DataSet</DataProvider><ConnectString>/* Local Connection */</ConnectString></ConnectionProperties></DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name=""Items"">
      <Query><DataSourceName>DataSource1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
      <Fields>
        <Field Name=""Name""><DataField>Name</DataField></Field>
        <Field Name=""Quantity""><DataField>Quantity</DataField></Field>
        <Field Name=""Price""><DataField>Price</DataField></Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <Height>5in</Height>
    <ReportItems>
      <Textbox Name=""TitleBox"">
        <Top>0.1in</Top><Left>0.1in</Left><Height>0.4in</Height><Width>7.5in</Width>
        <Value>=Parameters!ReportTitle.Value</Value>
        <Style><FontSize>18pt</FontSize><FontWeight>Bold</FontWeight></Style>
      </Textbox>
      <Textbox Name=""DateBox"">
        <Top>0.5in</Top><Left>0.1in</Left><Height>0.25in</Height><Width>7.5in</Width>
        <Value>=""Generated: "" &amp; Parameters!GeneratedDate.Value</Value>
        <Style><FontSize>10pt</FontSize><Color>Gray</Color></Style>
      </Textbox>
      <Tablix Name=""ItemsTable"">
        <Top>1in</Top><Left>0.1in</Left><Height>2in</Height><Width>7.5in</Width>
        <TablixBody>
          <TablixColumns>
            <TablixColumn><Width>3in</Width></TablixColumn>
            <TablixColumn><Width>1.5in</Width></TablixColumn>
            <TablixColumn><Width>1.5in</Width></TablixColumn>
            <TablixColumn><Width>1.5in</Width></TablixColumn>
          </TablixColumns>
          <TablixRows>
            <TablixRow>
              <Height>0.25in</Height>
              <TablixCells>
                <TablixCell><CellContents><Textbox Name=""HeaderName""><Value>Name</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>LightGray</BackgroundColor></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""HeaderQty""><Value>Quantity</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>LightGray</BackgroundColor></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""HeaderPrice""><Value>Price</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>LightGray</BackgroundColor></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""HeaderTotal""><Value>Total</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>LightGray</BackgroundColor></Style></Textbox></CellContents></TablixCell>
              </TablixCells>
            </TablixRow>
            <TablixRow>
              <Height>0.25in</Height>
              <TablixCells>
                <TablixCell><CellContents><Textbox Name=""Name""><Value>=Fields!Name.Value</Value></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""Quantity""><Value>=Fields!Quantity.Value</Value></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""Price""><Value>=Fields!Price.Value</Value><Style><Format>C2</Format></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""Total""><Value>=Fields!Quantity.Value * Fields!Price.Value</Value><Style><Format>C2</Format></Style></Textbox></CellContents></TablixCell>
              </TablixCells>
            </TablixRow>
          </TablixRows>
        </TablixBody>
        <TablixColumnHierarchy><TablixMembers><TablixMember/><TablixMember/><TablixMember/><TablixMember/></TablixMembers></TablixColumnHierarchy>
        <TablixRowHierarchy>
          <TablixMembers>
            <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
            <TablixMember><Group Name=""Details""/></TablixMember>
          </TablixMembers>
        </TablixRowHierarchy>
        <DataSetName>Items</DataSetName>
      </Tablix>
    </ReportItems>
  </Body>
  <Width>8in</Width>
  <Page><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight><LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin><TopMargin>0.5in</TopMargin><BottomMargin>0.5in</BottomMargin></Page>
</Report>";
    }

    private static string CreateTrialBalanceReportDefinition()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <AutoRefresh>0</AutoRefresh>
  <ReportParameters>
    <ReportParameter Name=""CompanyName""><DataType>String</DataType></ReportParameter>
    <ReportParameter Name=""ReportDate""><DataType>String</DataType></ReportParameter>
  </ReportParameters>
  <DataSources>
    <DataSource Name=""DataSource1""><ConnectionProperties><DataProvider>System.Data.DataSet</DataProvider><ConnectString>/* Local */</ConnectString></ConnectionProperties></DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name=""Accounts"">
      <Query><DataSourceName>DataSource1</DataSourceName><CommandText>/* Local */</CommandText></Query>
      <Fields>
        <Field Name=""AccountCode""><DataField>AccountCode</DataField></Field>
        <Field Name=""AccountName""><DataField>AccountName</DataField></Field>
        <Field Name=""Debit""><DataField>Debit</DataField></Field>
        <Field Name=""Credit""><DataField>Credit</DataField></Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <Height>6in</Height>
    <ReportItems>
      <Textbox Name=""CompanyBox""><Top>0.1in</Top><Left>0.1in</Left><Height>0.35in</Height><Width>7.5in</Width><Value>=Parameters!CompanyName.Value</Value><Style><FontSize>16pt</FontSize><FontWeight>Bold</FontWeight><TextAlign>Center</TextAlign></Style></Textbox>
      <Textbox Name=""TitleBox""><Top>0.5in</Top><Left>0.1in</Left><Height>0.3in</Height><Width>7.5in</Width><Value>Trial Balance</Value><Style><FontSize>14pt</FontSize><TextAlign>Center</TextAlign></Style></Textbox>
      <Textbox Name=""DateBox""><Top>0.85in</Top><Left>0.1in</Left><Height>0.25in</Height><Width>7.5in</Width><Value>=Parameters!ReportDate.Value</Value><Style><FontSize>10pt</FontSize><TextAlign>Center</TextAlign><Color>Gray</Color></Style></Textbox>
      <Tablix Name=""AccountsTable"">
        <Top>1.3in</Top><Left>0.1in</Left><Height>3in</Height><Width>7.5in</Width>
        <TablixBody>
          <TablixColumns>
            <TablixColumn><Width>1in</Width></TablixColumn>
            <TablixColumn><Width>3in</Width></TablixColumn>
            <TablixColumn><Width>1.5in</Width></TablixColumn>
            <TablixColumn><Width>1.5in</Width></TablixColumn>
          </TablixColumns>
          <TablixRows>
            <TablixRow>
              <Height>0.3in</Height>
              <TablixCells>
                <TablixCell><CellContents><Textbox Name=""HdrCode""><Value>Code</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>#4472C4</BackgroundColor><Color>White</Color><PaddingLeft>5pt</PaddingLeft></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""HdrAccount""><Value>Account Name</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>#4472C4</BackgroundColor><Color>White</Color><PaddingLeft>5pt</PaddingLeft></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""HdrDebit""><Value>Debit</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>#4472C4</BackgroundColor><Color>White</Color><TextAlign>Right</TextAlign><PaddingRight>5pt</PaddingRight></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""HdrCredit""><Value>Credit</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>#4472C4</BackgroundColor><Color>White</Color><TextAlign>Right</TextAlign><PaddingRight>5pt</PaddingRight></Style></Textbox></CellContents></TablixCell>
              </TablixCells>
            </TablixRow>
            <TablixRow>
              <Height>0.25in</Height>
              <TablixCells>
                <TablixCell><CellContents><Textbox Name=""Code""><Value>=Fields!AccountCode.Value</Value><Style><PaddingLeft>5pt</PaddingLeft></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""Account""><Value>=Fields!AccountName.Value</Value><Style><PaddingLeft>5pt</PaddingLeft></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""Debit""><Value>=Fields!Debit.Value</Value><Style><Format>N2</Format><TextAlign>Right</TextAlign><PaddingRight>5pt</PaddingRight></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""Credit""><Value>=Fields!Credit.Value</Value><Style><Format>N2</Format><TextAlign>Right</TextAlign><PaddingRight>5pt</PaddingRight></Style></Textbox></CellContents></TablixCell>
              </TablixCells>
            </TablixRow>
            <TablixRow>
              <Height>0.3in</Height>
              <TablixCells>
                <TablixCell><CellContents><Textbox Name=""TotalLabel""><Value>Total</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>#D9E2F3</BackgroundColor><PaddingLeft>5pt</PaddingLeft></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""Empty""><Value></Value><Style><BackgroundColor>#D9E2F3</BackgroundColor></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""TotalDebit""><Value>=Sum(Fields!Debit.Value)</Value><Style><FontWeight>Bold</FontWeight><Format>N2</Format><TextAlign>Right</TextAlign><BackgroundColor>#D9E2F3</BackgroundColor><PaddingRight>5pt</PaddingRight></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""TotalCredit""><Value>=Sum(Fields!Credit.Value)</Value><Style><FontWeight>Bold</FontWeight><Format>N2</Format><TextAlign>Right</TextAlign><BackgroundColor>#D9E2F3</BackgroundColor><PaddingRight>5pt</PaddingRight></Style></Textbox></CellContents></TablixCell>
              </TablixCells>
            </TablixRow>
          </TablixRows>
        </TablixBody>
        <TablixColumnHierarchy><TablixMembers><TablixMember/><TablixMember/><TablixMember/><TablixMember/></TablixMembers></TablixColumnHierarchy>
        <TablixRowHierarchy>
          <TablixMembers>
            <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
            <TablixMember><Group Name=""Details""/></TablixMember>
            <TablixMember><KeepWithGroup>Before</KeepWithGroup></TablixMember>
          </TablixMembers>
        </TablixRowHierarchy>
        <DataSetName>Accounts</DataSetName>
      </Tablix>
    </ReportItems>
  </Body>
  <Width>8in</Width>
  <Page><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight><LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin><TopMargin>0.5in</TopMargin><BottomMargin>0.5in</BottomMargin></Page>
</Report>";
    }

    private static string CreateDataDrivenReportDefinition()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <AutoRefresh>0</AutoRefresh>
  <ReportParameters>
    <ReportParameter Name=""ReportTitle""><DataType>String</DataType></ReportParameter>
    <ReportParameter Name=""GeneratedBy""><DataType>String</DataType></ReportParameter>
  </ReportParameters>
  <DataSources>
    <DataSource Name=""DataSource1""><ConnectionProperties><DataProvider>System.Data.DataSet</DataProvider><ConnectString>/* Local */</ConnectString></ConnectionProperties></DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name=""Employees"">
      <Query><DataSourceName>DataSource1</DataSourceName><CommandText>/* Local */</CommandText></Query>
      <Fields>
        <Field Name=""EmployeeId""><DataField>EmployeeId</DataField></Field>
        <Field Name=""Name""><DataField>Name</DataField></Field>
        <Field Name=""Department""><DataField>Department</DataField></Field>
        <Field Name=""Salary""><DataField>Salary</DataField></Field>
        <Field Name=""HireDate""><DataField>HireDate</DataField></Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <Height>6in</Height>
    <ReportItems>
      <Textbox Name=""TitleBox""><Top>0.1in</Top><Left>0.1in</Left><Height>0.4in</Height><Width>7.5in</Width><Value>=Parameters!ReportTitle.Value</Value><Style><FontSize>18pt</FontSize><FontWeight>Bold</FontWeight></Style></Textbox>
      <Textbox Name=""ByBox""><Top>0.55in</Top><Left>0.1in</Left><Height>0.25in</Height><Width>7.5in</Width><Value>=""Generated by: "" &amp; Parameters!GeneratedBy.Value &amp; "" on "" &amp; Format(Now(), ""MMMM dd, yyyy"")</Value><Style><Color>Gray</Color></Style></Textbox>
      <Tablix Name=""EmployeesTable"">
        <Top>1in</Top><Left>0.1in</Left><Height>3.5in</Height><Width>7.5in</Width>
        <TablixBody>
          <TablixColumns>
            <TablixColumn><Width>0.5in</Width></TablixColumn>
            <TablixColumn><Width>2in</Width></TablixColumn>
            <TablixColumn><Width>1.5in</Width></TablixColumn>
            <TablixColumn><Width>1.5in</Width></TablixColumn>
            <TablixColumn><Width>1.5in</Width></TablixColumn>
          </TablixColumns>
          <TablixRows>
            <TablixRow>
              <Height>0.3in</Height>
              <TablixCells>
                <TablixCell><CellContents><Textbox Name=""HdrId""><Value>ID</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>#2E7D32</BackgroundColor><Color>White</Color><TextAlign>Center</TextAlign></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""HdrName""><Value>Name</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>#2E7D32</BackgroundColor><Color>White</Color><PaddingLeft>5pt</PaddingLeft></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""HdrDept""><Value>Department</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>#2E7D32</BackgroundColor><Color>White</Color><PaddingLeft>5pt</PaddingLeft></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""HdrSalary""><Value>Salary</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>#2E7D32</BackgroundColor><Color>White</Color><TextAlign>Right</TextAlign><PaddingRight>5pt</PaddingRight></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""HdrHire""><Value>Hire Date</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>#2E7D32</BackgroundColor><Color>White</Color><TextAlign>Center</TextAlign></Style></Textbox></CellContents></TablixCell>
              </TablixCells>
            </TablixRow>
            <TablixRow>
              <Height>0.25in</Height>
              <TablixCells>
                <TablixCell><CellContents><Textbox Name=""Id""><Value>=Fields!EmployeeId.Value</Value><Style><TextAlign>Center</TextAlign></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""Name""><Value>=Fields!Name.Value</Value><Style><PaddingLeft>5pt</PaddingLeft></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""Dept""><Value>=Fields!Department.Value</Value><Style><PaddingLeft>5pt</PaddingLeft></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""Salary""><Value>=Fields!Salary.Value</Value><Style><Format>C0</Format><TextAlign>Right</TextAlign><PaddingRight>5pt</PaddingRight></Style></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""Hire""><Value>=Fields!HireDate.Value</Value><Style><Format>MMM dd, yyyy</Format><TextAlign>Center</TextAlign></Style></Textbox></CellContents></TablixCell>
              </TablixCells>
            </TablixRow>
          </TablixRows>
        </TablixBody>
        <TablixColumnHierarchy><TablixMembers><TablixMember/><TablixMember/><TablixMember/><TablixMember/><TablixMember/></TablixMembers></TablixColumnHierarchy>
        <TablixRowHierarchy>
          <TablixMembers>
            <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
            <TablixMember><Group Name=""Details""/></TablixMember>
          </TablixMembers>
        </TablixRowHierarchy>
        <DataSetName>Employees</DataSetName>
      </Tablix>
      <Textbox Name=""CountBox""><Top>4.6in</Top><Left>0.1in</Left><Height>0.25in</Height><Width>7.5in</Width><Value>=""Total Employees: "" &amp; CountRows(""Employees"")</Value><Style><FontWeight>Bold</FontWeight></Style></Textbox>
      <Textbox Name=""AvgBox""><Top>4.9in</Top><Left>0.1in</Left><Height>0.25in</Height><Width>7.5in</Width><Value>=""Average Salary: "" &amp; Format(Avg(Fields!Salary.Value, ""Employees""), ""C0"")</Value><Style><FontWeight>Bold</FontWeight></Style></Textbox>
    </ReportItems>
  </Body>
  <Width>8in</Width>
  <Page><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight><LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin><TopMargin>0.5in</TopMargin><BottomMargin>0.5in</BottomMargin></Page>
</Report>";
    }

    #endregion

    #region Event Handlers

    private async void OnRenderingComplete(object? sender, MauiModels.ReportRenderingCompleteEventArgs e)
    {
        if (e.Success)
        {
            System.Diagnostics.Debug.WriteLine($"Report rendered successfully with {e.TotalPages} pages.");
        }
        else
        {
            await DisplayAlert("Rendering Failed", "The report could not be rendered.", "OK");
        }
    }

    private async void OnReportError(object? sender, MauiModels.ReportErrorEventArgs e)
    {
        await DisplayAlert("Report Error", e.Exception.Message, "OK");
        e.Handled = true;
    }

    private void OnExportRequested(object? sender, MauiModels.ReportExportRequestedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Export requested: {e.Format}");
    }

    #endregion
}
