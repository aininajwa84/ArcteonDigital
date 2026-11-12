# Arcteon Digital - ASP.NET Core Razor Pages

## Run in Visual Studio

1. Install a Visual Studio version that supports .NET 10, with the ASP.NET and web development workload and .NET 10 SDK.
2. Open Dewi.sln.
3. Select Dewi as the startup project and the Dewi launch profile.
4. Press F5 (debug) or Ctrl+F5 (run). The browser opens http://localhost:5080.

You can also run `dotnet run --project Dewi.csproj` in this folder.

## Project structure

- Pages: all four original pages converted to .cshtml Razor pages with C# PageModel files.
- Pages/Shared/_Layout.cshtml: shared navigation, footer, styles and scripts.
- Email: contact and career form handlers, server validation, and SMTP delivery.
- wwwroot/assets: original CSS, JavaScript, fonts and images, preserved in their browser formats.
- Properties/launchSettings.json: Visual Studio localhost launch profile.

Routes: /, /portfolio-details, /service-details, /starter-page.

## SMTP setup

The contact form emails `enquiry@arcteondigital.com.my`. Career applications and their résumé attachments go to `jabkitol.jabar@arcteondigital.com`. Replies go to the address entered by the visitor.

Set `Smtp__Host`, `Smtp__FromAddress`, `Smtp__Username` and `Smtp__Password` in the hosting environment. Use a sender address authorised by the provider. Keep the password in the hosting secret store. For a relay that does not require a login, leave both username and password unset.

The defaults are `Smtp__Port=587` and `Smtp__Security=StartTls`. For a provider that uses implicit TLS, set `Smtp__Port=465` and `Smtp__Security=SslOnConnect`. The website reports delivery as unavailable until the SMTP settings are configured.

Google Fonts, map embeds and video links still require internet access. BootstrapMade template attribution and original licensing comments are retained; see Readme.txt.

## Digital design update

The homepage adapts the service catalogue and expandable service information from https://arcteon-digital.com/ and the people-first introduction, product categories and partnership themes from https://mindful-resources.com/. Arcteon Digital is the company identity; Dewi is the underlying Bootstrap template. The site uses adapted layouts, original wording and existing licensed imagery.

The six solution cards link to /service-details?solution=crm (or clinic, queue, hr, assets, school), with C#-backed descriptions and proposed capabilities. These describe solution areas, not completed application products. Services expand using keyboard-accessible native disclosure controls. Styles are in wwwroot/assets/css/digital.css.

Company branding and contact details use the published Arcteon Digital website. If Visual Studio reports a locked build file, stop the running Dewi debugging session before rebuilding.

## Core services

Arcteon Digital focuses on AI, Software Customization and Consultation. These three services are reflected in the homepage hero, architecture illustration, expandable service cards, footer links and service overview. Business solution cards illustrate potential application areas for these services.

## Contact and job enquiries

- `/careers` opens the homepage careers section, where visitors can submit an application.
- Clicking the address in the contact section or footer opens a Bootstrap map dialog. The directions button opens Google Maps in a new tab. Map rendering requires internet access.
- Telephone links open a dialer; email links open an email draft. Neither sends a message automatically.
- Working hours: Monday–Friday, 9.00 am–6.00 pm.

## Glass styling

The original navy/orange layout now uses glass.css for translucent cards, blurred navigation, subtle tinted backgrounds and frosted forms. Direct job-enquiry links scroll to the enquiry tabs, including on mobile. Static styles and scripts use versioned URLs to refresh browser caches.

## Careers & internships preview

The homepage Careers section includes three explicitly labeled sample roles, internship/full-time filters and a general-interest action. Applications open in a responsive popup with name, email, phone, interest, organization, availability, résumé and optional cover note. These roles are examples, not confirmed vacancies.

Applications post to `/forms/applications` with antiforgery protection. Applicants can attach one required résumé and up to three optional supporting documents. Each file must be PDF, DOC or DOCX and at most 5 MB, with a combined limit of 15 MB. The server validates the application and sends its attachments in one email to HR. The contact form posts to `/forms/contact` with the same antiforgery protection.
