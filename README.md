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
- Pages/Forms: C# replacement for both PHP handlers, with server validation and antiforgery protection.
- wwwroot/assets: original CSS, JavaScript, fonts and images, preserved in their browser formats.
- Properties/launchSettings.json: Visual Studio localhost launch profile.

Routes: /, /portfolio-details, /service-details, /starter-page.

Contact and newsletter submissions are saved as individual JSON files in App_Data/contact and App_Data/newsletter. These folders are outside the public web root. This is a local demo: no email is sent and no mailing-list service is connected. Configure an email/newsletter provider before using those features in production. Do not commit personal submissions.

Google Fonts, map embeds and video links still require internet access. BootstrapMade template attribution and original licensing comments are retained; see Readme.txt.

## Digital design update

The homepage adapts the service catalogue and expandable service information from https://arcteon-digital.com/ and the people-first introduction, product categories and partnership themes from https://mindful-resources.com/. Arcteon Digital is the company identity; Dewi is the underlying Bootstrap template. The site uses adapted layouts, original wording and existing licensed imagery.

The six solution cards link to /service-details?solution=crm (or clinic, queue, hr, assets, school), with C#-backed descriptions and proposed capabilities. These describe solution areas, not completed application products. Services expand using keyboard-accessible native disclosure controls. Styles are in wwwroot/assets/css/digital.css.

Company branding and contact details use the published Arcteon Digital website. If Visual Studio reports a locked build file, stop the running Dewi debugging session before rebuilding.

## Core services

Arcteon Digital focuses on AI, Software Customization and Consultation. These three services are reflected in the homepage hero, architecture illustration, expandable service cards, footer links and service overview. Business solution cards illustrate potential application areas for these services.

## Contact and job enquiries

- /careers: general job enquiry form with name, email, role/interest, optional portfolio URL and message. It does not advertise current vacancies.
- Job enquiries are validated in C# and saved under App_Data/careers. They are not emailed; connect a delivery provider before going live.
- Clicking the address in the contact section or footer opens a Bootstrap map dialog. The directions button opens Google Maps in a new tab. Map rendering requires internet access.
- Telephone links open a dialer; email links open an email draft. Neither sends a message automatically.
- Working hours: Monday–Friday, 9.00 am–6.00 pm.

The homepage Contact section includes Project Enquiry and Job Enquiry tabs. Navigation and contact-card job links open /#contact-job directly. Both the Contact tab and /careers use the same shared job form and C# submission handler.

## Glass styling

The original navy/orange layout now uses glass.css for translucent cards, blurred navigation, subtle tinted backgrounds and frosted forms. Direct job-enquiry links scroll to the enquiry tabs, including on mobile. Static styles and scripts use versioned URLs to refresh browser caches.

The Contact enquiry tabs have been replaced by an Enquiry Type dropdown (Project Enquiry / Job Enquiry). Selecting an option switches the form and disables hidden fields. The URL reflects the selected type; /#contact-job still opens the job form directly. Both submission paths were checked in the running site.

Job enquiries are now available only through the homepage Contact dropdown. The separate menu/footer links and careers promotion card were removed; /careers redirects to the Contact job enquiry option. Submissions continue to be saved locally, with no email delivery configured.

## Careers & internships preview

The homepage Careers section includes three explicitly labeled sample roles, internship/full-time filters and a general-interest action. Applications open in a responsive popup with name, email, phone, interest, organization, availability, résumé and optional cover note. These roles are examples, not confirmed vacancies.

Applications POST to /forms/applications with antiforgery protection. Required fields, allowed roles, consent, file extension, signature and 5 MB résumé size are validated server-side. Files receive generated names and are stored with application JSON under App_Data/applications outside wwwroot. No email is sent. Connect HR delivery, replace the sample roles and add your actual recruitment/privacy terms before production use. File signature checks are basic format checks, not antivirus scanning.
