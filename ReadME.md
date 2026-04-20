# Naukri Resume Auto-Refresher

A .NET 8 console app that auto-refreshes your Naukri profile daily to boost recruiter visibility.

## Setup
1. Copy `appsettings.example.json` and rename to `appsettings.json`
2. Fill in your `Email`, `Password`, and `FormKey`
3. Place your resume PDF in the `Resume/` folder
4. Add your own phrases in `SummarySwaps` and `HeadlineSwaps`
5. Run: `dotnet run`

## Getting FormKey
Naukri → DevTools → Network tab → upload resume once → find `filevalidation.naukri.com/file` request → copy `formKey`.