using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dewi.Pages;

public record ProductDetailContent(string Heading, string Enquiry, string[] FeatureDescriptions);

public class ServiceDetailsModel : PageModel
{
    public BusinessSolution? Solution { get; private set; }
    public bool HasNexusBrochure => Solution?.Key == "nexus";
    public (string File, string Title)? Brochure => Solution?.Key switch
    {
        "clinic" => ("arcteonmed-brochure.jpeg", "ArcteonMed Clinic Management System"),
        "queue" => ("byondq-smart-queue-brochure.png", "ByondQ Smart Queue Management System"),
        _ => null
    };
    public IReadOnlyList<BusinessSolution> Products => IndexModel.Catalogue.Where(item => !item.IsClientSolution).ToArray();
    public ProductDetailContent DetailContent => Solution?.Key switch
    {
        "hr" => new("Support for your everyday HR work.", "Tell us how you manage HR today. We’ll help you define a practical approach for your team.", [
            "Organize employee information and support the administrative tasks that keep your team running.",
            "Manage attendance processes around your working arrangements and the information your HR team needs.",
            "Bring structure to routine HR tasks with workflows suited to your people and business requirements."]),
        "clinic" => new("Support the work behind patient care.", "Tell us about your clinic’s daily processes. We’ll help you clarify the workflows and system requirements.", [
            "Support the administrative steps involved in managing patient information and coordinating patient-related work.",
            "Organize the day-to-day administrative processes that help your clinic operate.",
            "Plan how patient and administrative workflows can work together around your clinic’s needs."]),
        "assets" => new("Manage your software licences and computers.", "Tell us how you manage software licences and computers today. We’ll discuss your licensing, administration and monitoring needs.", [
            "Organize software licence information to support your organization’s licensing and administration needs.",
            "Manage computer information around your organization’s device policies and operational requirements.",
            "Clarify the device information and monitoring workflows your IT team needs to support daily operations."]),
        "tender" => new("Automate your tender and quotation workflows with AI.", "Walk us through your tender and quotation process. We’ll discuss how AI workflow automation can support your team.", [
            "Apply AI workflow automation to support the processing steps in your tender and quotation workflows.",
            "Organize tender information, quotations and the administrative steps involved in managing submissions.",
            "Connect the people, information and tasks involved in your procurement process."]),
        "rappovena" => new("A website for RappoVena’s online presence.", "Tell us about your business and what you need from your website. We’ll discuss the content, structure and design.", [
            "Develop a corporate website that introduces the medical supply business online.",
            "Present company information in a clear website structure for visitors.",
            "Give the business a dedicated online presence that reflects its identity."]),
        "queue" => new("Keep your service queues organized.", "Tell us about your service counters and customer flow. We’ll help you plan the right queue and ticket workflows.", [
            "Plan how customers move through your service process, from arrival to the appropriate service point.",
            "Organize queue handling around your counters, service types and operational needs.",
            "Support the ticket processes your team uses to coordinate customers and service queues."]),
        "nexus" => new("Connect your business, one module at a time.", "Tell us which business process you want to improve first. We’ll discuss the right starting module and how to connect your next workflow.", [
            "Convert paper documents into searchable digital records. Find files by name, date or reference, use AI to find information faster and access records securely through the cloud.",
            "Keep customer details and history together, track enquiries and sales progress, and set reminders for follow-up.",
            "Track stock coming in and going out, use barcode scanning, review stock balances and receive stock reports.",
            "Promote products online, accept orders around the clock, create bills and receipts, and connect a secure payment gateway.",
            "Use digital forms, route approvals to the right person, notify staff automatically and keep a clear approval history."]),
        _ => new("Connected technology for your business.", "Tell us about your requirements and the way your team works.", [])
    };
    public (string File, string Alt) DetailPhoto => Solution?.Key switch
    {
        "hr" => ("it-team-collaboration.jpg", "Colleagues reviewing work together at a computer in an office"),
        "clinic" => ("clinic-digital-workflow.jpg", "Healthcare professional using a computer beside a stethoscope"),
        "assets" => ("malaysia-workspace.jpg", "Hands working on a laptop in Kuala Lumpur"),
        "tender" => ("malaysia-business-meeting.jpg", "Business colleagues discussing information on a phone in a meeting room"),
        "rappovena" => ("digital-business-workflow.jpg", "Colleagues reviewing work together on a laptop"),
        "queue" => ("workplace-planning.jpg", "Colleagues in hijabs presenting workflow data beside laptops"),
        "nexus" => ("digital-business-workflow.jpg", "Colleagues reviewing business information on a laptop"),
        _ => ("it-team-collaboration.jpg", "Colleagues collaborating at a computer in a technology workspace")
    };
    public IActionResult OnGet(string? solution)
    {
        if (solution is null) return Page();
        Solution = IndexModel.Catalogue.FirstOrDefault(item => item.Key == solution);
        return Solution is null ? NotFound() : Page();
    }
}
