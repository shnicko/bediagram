using BeDiagram.Components;
using BeDiagram.Components.Pages.Commands;
using BeDiagram.Services;
using BeDiagram.Services.Export;
using BeDiagram.Services.Export.Pdf;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Diagrams are stored as JSON files in a local folder (see appsettings.json -> Storage:Folder).
builder.Services.AddSingleton<DiagramStore>();

// Export strategies (see Services/Export). The Export menu lists formats and areas in the order registered here.
// Formats: what kind of file. Areas: which part of the drawing (whole page / extents, or the current view).
builder.Services.AddSingleton<SvgDiagramWriter>();   // the shared SVG renderer behind SVG, PNG and PDF
builder.Services.AddSingleton<PdfWriter>();          // SVG → PDF
builder.Services.AddSingleton<IExportFormat, SvgExportFormat>();
builder.Services.AddScoped<IExportFormat, PngExportFormat>();     // scoped: draws in this user's browser
builder.Services.AddSingleton<IExportFormat, PdfExportFormat>();
builder.Services.AddSingleton<IExportFormat, MermaidExportFormat>();
builder.Services.AddSingleton<IExportArea, WholeDrawingArea>();
builder.Services.AddSingleton<IExportArea, CurrentViewArea>();
builder.Services.AddScoped<IPngRasterizer, BrowserPngRasterizer>();
builder.Services.AddScoped<ExportService>();

// Editor commands (see Components/Pages/Commands): every keyboard shortcut and the toolbar / panel actions.
builder.Services.AddSingleton<CommandRegistry>(CommandRegistry.CreateDefault());

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
