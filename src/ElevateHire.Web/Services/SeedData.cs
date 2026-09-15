using ElevateWorkforce.Domain.Entities;
using ElevateWorkforce.Domain.Enums;
using ElevateWorkforce.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ElevateWorkforce.Web.Services;

public static class SeedData
{
    private const string DemoCompanyName = "Elevate Workforce Solutions";
    private const string LegacyDemoCompanyName = "ElevateWorkforce Corp";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        foreach (var role in new[] { "Admin", "Employer", "JobSeeker" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new ApplicationRole { Name = role });
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Failed to seed role {role}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
            }
        }

        await SeedUsersAsync(userManager, context);
        await SeedCompaniesAsync(context);
        await SeedJobsAsync(context);
        await SeedApplicationsAsync(context);
        await EnsureDemoEmployerApplicationsAsync(context);
    }

    private static async Task SeedUsersAsync(UserManager<User> userManager, ApplicationDbContext context)
    {
        if (await context.JobSeekers.AnyAsync()) return;

        var admin = await EnsureUserAsync(userManager, "admin@elevateworkforce.local", "Aarav Sharma", "Admin", "Admin@123");
        admin.Role = UserRole.Admin;
        await EnsureRoleMembershipAsync(userManager, admin, "Admin");
        await userManager.UpdateAsync(admin);

        var employer = await EnsureUserAsync(userManager, "employer@elevateworkforce.local", "Priya Adhikari", "Employer", "Employer@123");
        employer.Role = UserRole.Employer;
        await EnsureRoleMembershipAsync(userManager, employer, "Employer");
        await userManager.UpdateAsync(employer);

        var jobSeeker = await EnsureUserAsync(userManager, "jobseeker@elevateworkforce.local", "Suman Karki", "JobSeeker", "Jobseeker@123");
        jobSeeker.Role = UserRole.JobSeeker;
        await EnsureRoleMembershipAsync(userManager, jobSeeker, "JobSeeker");
        await userManager.UpdateAsync(jobSeeker);

        var seekers = new List<JobSeeker>
        {
            NewSeeker(jobSeeker, "Frontend Developer", "Kathmandu", "I build clean, accessible web interfaces with React and modern CSS. 3 years of experience shipping products for Nepal-based startups.",
                "React, JavaScript, TypeScript, CSS, HTML, Git", "BSc in Computer Science, Tribhuvan University",
                "Frontend Developer at a fintech startup (2021-now); Junior Web Developer at a digital agency (2019-2021)",
                "Meta Frontend Developer Certificate", "Nepali, English"),
            NewSeeker(await SeekUserAsync(userManager, "anita.gurung@gmail.com", "Anita Gurung", "JobSeeker", "Jobseeker@123"), "Data Analyst", "Lalitpur", "Detail-oriented analyst turning messy data into clear decisions using SQL, Python and Excel.",
                "SQL, Python, Excel, Power BI, Statistics", "MBA, Kathmandu University",
                "Data Analyst at a telecom company (2020-now)", "Google Data Analytics Professional Certificate", "Nepali, English, Hindi"),
            NewSeeker(await SeekUserAsync(userManager, "bibek.tamang@gmail.com", "Bibek Tamang", "JobSeeker", "Jobseeker@123"), "Backend Developer", "Pokhara", "Backend engineer focused on scalable API design with .NET and PostgreSQL.",
                "C#, ASP.NET Core, PostgreSQL, Docker, Redis", "BEng in Software Engineering, Pokhara University",
                "Backend Developer at an e-commerce platform (2019-now)", "Microsoft Certified: Azure Developer Associate", "Nepali, English"),
            NewSeeker(await SeekUserAsync(userManager, "meera.shrestha@gmail.com", "Meera Shrestha", "JobSeeker", "Jobseeker@123"), "UI/UX Designer", "Bhaktapur", "Product designer who believes good design is invisible. Focused on fintech and consumer apps.",
                "Figma, UX Research, Wireframing, Prototyping, Design Systems", "BDes in Interaction Design, Kathmandu University",
                "Product Designer at a bank's digital unit (2021-now)", "Google UX Design Certificate", "Nepali, English"),
            NewSeeker(await SeekUserAsync(userManager, "roshan.kc@gmail.com", "Roshan K.C.", "JobSeeker", "Jobseeker@123"), "DevOps Engineer", "Kathmandu", "Automation enthusiast managing CI/CD pipelines and cloud infrastructure for growing teams.",
                "Docker, Kubernetes, AWS, Terraform, CI/CD, Linux", "BSc in IT, Purbanchal University",
                "DevOps Engineer at an ISP (2020-now)", "AWS Certified Solutions Architect - Associate", "Nepali, English"),
            NewSeeker(await SeekUserAsync(userManager, "sunita.rai@gmail.com", "Sunita Rai", "JobSeeker", "Jobseeker@123"), "Digital Marketing Specialist", "Biratnagar", "Marketing professional who connects brands with the right audience through content and campaigns.",
                "SEO, Google Ads, Social Media, Content Strategy, Analytics", "BA in Mass Communication, Purbanchal University",
                "Digital Marketing Executive at a retail group (2021-now)", "Google Ads Certification", "Nepali, English, Maithili"),
            NewSeeker(await SeekUserAsync(userManager, "prakshit.thapa@gmail.com", "Prakash Thapa", "JobSeeker", "Jobseeker@123"), "Cyber Security Analyst", "Chitwan", "Security analyst focused on threat monitoring, incident response and security awareness.",
                "Network Security, SIEM, Penetration Testing, Incident Response", "BSc in Cybersecurity, Kathmandu University",
                "Security Analyst at an audit firm (2020-now)", "CompTIA Security+", "Nepali, English"),
            NewSeeker(await SeekUserAsync(userManager, "isha.shah@gmail.com", "Isha Shah", "JobSeeker", "Jobseeker@123"), "Accountant", "Butwal", "Chartered accountant candidate experienced with Nepali taxation and financial reporting.",
                "Tally, QuickBooks, Taxation, Financial Reporting, Excel", "MSc in Accounting, Tribhuvan University",
                "Accountant at a manufacturing firm (2018-now)", "Member, ICAN (Associate)", "Nepali, English, Hindi"),
            NewSeeker(await SeekUserAsync(userManager, "dipesh.maharjan@gmail.com", "Dipesh Maharjan", "JobSeeker", "Jobseeker@123"), "Customer Support Representative", "Dharan", "Friendly support professional who resolves customer issues quickly and creates great experiences.",
                "Customer Service, CRM, Communication, Problem Solving", "BA in English, Purbanchal University",
                "Customer Support Executive at a bank (2021-now)", "IELTS 7.0", "Nepali, English"),
            NewSeeker(await SeekUserAsync(userManager, "kabita.neupane@gmail.com", "Kabita Neupane", "JobSeeker", "Jobseeker@123"), "HR Officer", "Kathmandu", "HR generalist supporting recruitment, onboarding and employee engagement across teams.",
                "Recruitment, HRIS, Employee Relations, Training", "MBA in HR, Kathmandu University",
                "HR Officer at a hospital group (2020-now)", "Certified HR Professional, NHRAN", "Nepali, English"),
            NewSeeker(await SeekUserAsync(userManager, "saroj.sanjay@gmail.com", "Saroj Pandey", "JobSeeker", "Jobseeker@123"), "Sales Executive", "Lalitpur", "Goal-driven sales professional with a track record of exceeding targets in B2B and retail.",
                "B2B Sales, Negotiation, CRM, Market Research", "BBA, Nepal Commerce Campus", 
                "Sales Executive at a distribution company (2019-now)", "Salesforce Administrator (Beginner)", "Nepali, English, Hindi"),
            NewSeeker(await SeekUserAsync(userManager, "rashmi.sitaula@gmail.com", "Rashmi Sitaula", "JobSeeker", "Jobseeker@123"), "Data Scientist", "Kathmandu", "Data scientist building predictive models that drive business decisions across industries.",
                "Python, Machine Learning, TensorFlow, Statistics, Spark", "MSc in Data Science, Kathmandu University",
                "Data Scientist at a health-tech startup (2021-now)", "Kaggle Expert", "Nepali, English"),
        };

        await context.JobSeekers.AddRangeAsync(seekers);

        var company = context.Companies.Local.FirstOrDefault(c => c.Name == DemoCompanyName) ?? new Company
        {
            Name = DemoCompanyName,
            Industry = "Technology",
            Description = "Demo company for Elevate Workforce Solutions.",
            Location = "Kathmandu",
            Status = CompanyStatus.Active
        };

        var employerEntity = new Employer { User = employer, Company = company, Position = "HR Manager", Phone = "9801234567" };
        await context.Employers.AddAsync(employerEntity);

        var hrNepal = new Company
        {
            Name = "Himalayan Tech Solutions",
            Industry = "Software Development",
            Description = "A Kathmandu-based software house building products for clients across Southeast Asia.",
            Website = "https://himalayantech.example.com",
            Location = "Kathmandu",
            ContactEmail = "careers@himalayantech.example.com",
            ContactPhone = "01-4445566",
            Status = CompanyStatus.Active
        };
        await context.Companies.AddAsync(hrNepal);

        await context.SaveChangesAsync();
    }

    private static JobSeeker NewSeeker(User user, string title, string location, string about, string skills, string education, string experience, string certs, string languages) =>
        new()
        {
            User = user,
            ProfessionalTitle = title,
            Location = location,
            About = about,
            Skills = skills,
            Education = education,
            Experience = experience,
            Certifications = certs,
            Languages = languages
        };

    private static async Task<User> EnsureUserAsync(UserManager<User> userManager, string email, string fullName, string _, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null) return user;
        user = new User { FullName = fullName, Email = email, UserName = email, Role = UserRole.JobSeeker };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to seed user {email}: {string.Join("; ", result.Errors)}");
        return user;
    }

    private static async Task<User> SeekUserAsync(UserManager<User> userManager, string email, string fullName, string role, string password)
    {
        var user = await EnsureUserAsync(userManager, email, fullName, role, password);
        if (!await userManager.IsInRoleAsync(user, role))
        {
            var r = await userManager.AddToRoleAsync(user, role);
            if (!r.Succeeded)
                throw new InvalidOperationException($"Failed to add role to {email}");
        }
        user.Role = role == "JobSeeker" ? UserRole.JobSeeker : role == "Employer" ? UserRole.Employer : UserRole.Admin;
        await userManager.UpdateAsync(user);
        return user;
    }

    private static async Task EnsureRoleMembershipAsync(UserManager<User> userManager, User user, string role)
    {
        if (await userManager.IsInRoleAsync(user, role)) return;

        var result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to add role {role} to {user.Email}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
    }

    private static async Task SeedCompaniesAsync(ApplicationDbContext context)
    {
        var legacyCompany = await context.Companies.SingleOrDefaultAsync(c => c.Name == LegacyDemoCompanyName);
        if (legacyCompany is not null)
        {
            legacyCompany.Name = DemoCompanyName;
            await context.SaveChangesAsync();
        }

        if (await context.Companies.AnyAsync(c => c.Name != "Himalayan Tech Solutions" && c.Name != DemoCompanyName)) return;

        var companies = new[]
        {
            new Company
            {
                Name = "F1Soft International", Industry = "Fintech", Location = "Kathmandu",
                Description = "Nepal's leading fintech company, building digital payment and banking solutions for millions of users.",
                Website = "https://f1soft.example.com", ContactEmail = "careers@f1soft.example.com", ContactPhone = "01-4006000",
                Status = CompanyStatus.Active
            },
            new Company
            {
                Name = "Yomari Tech", Industry = "Software Development", Location = "Lalitpur",
                Description = "Product studio building award-winning web and mobile products for international clients.",
                Website = "https://yomaritech.example.com", ContactEmail = "hello@yomaritech.example.com", ContactPhone = "01-5533000",
                Status = CompanyStatus.Active
            },
            new Company
            {
                Name = "Daraz Nepal", Industry = "E-commerce", Location = "Kathmandu",
                Description = "South Asia's leading online shopping marketplace with a strong presence in Nepal.",
                Website = "https://daraz.example.com", ContactEmail = "jobs@daraz.example.com", ContactPhone = "01-5970123",
                Status = CompanyStatus.Active
            },
            new Company
            {
                Name = "CloudFactory", Industry = "AI & Data Services", Location = "Lalitpur",
                Description = "Global company providing quality AI training data and digital work opportunities to talent in Nepal.",
                Website = "https://cloudfactory.example.com", ContactEmail = "careers@cloudfactory.example.com", ContactPhone = "01-5524000",
                Status = CompanyStatus.Active
            },
            new Company
            {
                Name = "Khalti Digital Wallet", Industry = "Payments", Location = "Kathmandu",
                Description = "Digital wallet and payment gateway enabling digital payments across Nepal.",
                Website = "https://khalti.example.com", ContactEmail = "jobs@khalti.example.com", ContactPhone = "01-4567000",
                Status = CompanyStatus.Active
            },
            new Company
            {
                Name = "Chandragiri Hills", Industry = "Tourism & Hospitality", Location = "Kathmandu",
                Description = "Nepal's premier cable-car and hospitality destination serving visitors from around the world.",
                Website = "https://chandragiri.example.com", ContactEmail = "hr@chandragiri.example.com", ContactPhone = "01-4203888",
                Status = CompanyStatus.PendingVerification
            },
            new Company
            {
                Name = "Nepal SBI Bank", Industry = "Banking", Location = "Kathmandu",
                Description = "A joint venture commercial bank providing banking services across Nepal.",
                Website = "https://nepalsbi.example.com", ContactEmail = "careers@nepalsbi.example.com", ContactPhone = "01-4421000",
                Status = CompanyStatus.Active
            },
        };

        foreach (var company in companies)
        {
            if (!await context.Companies.AnyAsync(c => c.Name == company.Name))
                await context.Companies.AddAsync(company);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedJobsAsync(ApplicationDbContext context)
    {
        if (await context.Jobs.AnyAsync()) return;

        var companies = await context.Companies
            .Where(c => c.Status == CompanyStatus.Active)
            .ToDictionaryAsync(c => c.Name);

        var now = DateTime.UtcNow;
        var jobs = new List<Job>();

        void Add(string title, string company, JobCategory category, JobType type, ExperienceLevel exp,
            string location, decimal min, decimal max, bool featured,
            string description, string responsibilities, string requirements, string qualifications, string skills, string benefits,
            int vacancies, bool remote, JobStatus status, DateTime published, DateTime deadline) =>
            jobs.Add(new Job
            {
                Title = title,
                CompanyId = companies.TryGetValue(company, out var c) ? c.Id : companies.Values.First().Id,
                Category = category,
                JobType = type,
                ExperienceLevel = exp,
                Location = location,
                MinSalary = min,
                MaxSalary = max,
                IsRemote = remote,
                Description = description,
                Responsibilities = responsibilities,
                Requirements = requirements,
                Qualifications = qualifications,
                Skills = skills,
                Benefits = benefits,
                Vacancies = vacancies,
                Status = status,
                PublishedAt = published,
                Deadline = deadline,
                IsFeatured = featured,
                CreatedAt = published
            });

        Add("Software Engineer", "F1Soft International", JobCategory.SoftwareDevelopment, JobType.FullTime, ExperienceLevel.MidLevel, "Kathmandu", 60000, 90000, true,
            "Build and maintain core banking and payment products used by millions of customers. Work with a talented engineering team to ship reliable, secure software.",
            "Design and develop new features\nWrite clean, testable code\nCollaborate with product and design\nParticipate in code reviews\nMaintain documentation",
            "3+ years experience with C#/.NET or Java\nStrong knowledge of SQL and REST APIs\nExperience with microservices a plus",
            "BSc in Computer Science or equivalent", "C#, .NET, SQL, REST, Git", "Health insurance, performance bonus, gym membership, learning budget",
            40, true, JobStatus.Active, now.AddDays(-12), now.AddDays(35).Date);

        Add("Backend Developer", "Himalayan Tech Solutions", JobCategory.SoftwareDevelopment, JobType.FullTime, ExperienceLevel.MidLevel, "Kathmandu", 55000, 80000, true,
            "Build scalable backend services powering our SaaS products for international markets.",
            "Develop and maintain backend services\nOptimize database queries\nIntegrate third-party APIs\nWrite unit and integration tests",
            "2-4 years in backend development\nSolid understanding of PostgreSQL\nExperience with cloud deployment",
            "BSc in Computer Science or related", "C#, ASP.NET Core, PostgreSQL, Docker", "Flexible working hours, remote days, festival bonuses", 30, false, JobStatus.Active, now.AddDays(-8), now.AddDays(40).Date);

        Add("UI/UX Designer", "Yomari Tech", JobCategory.Design, JobType.FullTime, ExperienceLevel.MidLevel, "Lalitpur", 45000, 70000, true,
            "Design delightful, accessible interfaces for consumer and enterprise products shipped globally.",
            "Own end-to-end design process\nCreate wireframes and prototypes\nBuild and maintain design systems\nRun user testing sessions",
            "3+ years of product design experience\nAdvanced Figma skills\nPortfolio showing shipped work",
            "Degree in Design or related field", "Figma, FigJam, UX Research, Prototyping", "Design conference budget, health cover, snack-filled office", 2, true, JobStatus.Active, now.AddDays(-16), now.AddDays(25).Date);

        Add("Data Analyst", "Daraz Nepal", JobCategory.Data, JobType.FullTime, ExperienceLevel.Entry, "Kathmandu", 35000, 50000, true,
            "Analyze marketplace data to uncover insights that drive commercial decisions across the platform.",
            "Build dashboards in Power BI\nAnalyze seller and customer behaviour\nPresent findings to stakeholders\nAutomate recurring reports",
            "1-2 years in analytics\nStrong SQL skills\nGood communication",
            "BSc in Statistics, Maths or Economics", "SQL, Excel, Power BI, Python", "Market-competitive pay, staff discounts, growth plans", 30, false, JobStatus.Active, now.AddDays(-5), now.AddDays(45).Date);

        Add("Data Scientist", "CloudFactory", JobCategory.Data, JobType.FullTime, ExperienceLevel.Senior, "Lalitpur", 120000, 160000, true,
            "Build ML models that improve the quality and efficiency of our AI training data pipelines.",
            "Develop machine learning models\nDesign data collection pipelines\nCollaborate with ML engineers\nCommunicate model results",
            "5+ years in data science\nProduction ML experience\nStrong python and statistics",
            "MSc/PhD in ML, Data Science or equivalent", "Python, ML, TensorFlow, PySpark", "Health insurance, yearly bonus, remote-friendly", 40, true, JobStatus.Active, now.AddDays(-20), now.AddDays(30).Date);

        Add("DevOps Engineer", "CloudFactory", JobCategory.SoftwareDevelopment, JobType.FullTime, ExperienceLevel.MidLevel, "Lalitpur", 90000, 130000, true,
            "Own our cloud infrastructure, CI/CD pipelines and developer experience.",
            "Manage AWS infrastructure with Terraform\nImprove CI/CD pipelines\nMonitor system health and costs\nAutomate operational tasks",
            "3+ years in DevOps or SRE\nStrong AWS and Linux experience\nKubernetes in production",
            "BSc in Computer Science or equivalent", "AWS, Terraform, Kubernetes, CI/CD", "Health insurance, learning stipend, conference budget", 25, false, JobStatus.Active, now.AddDays(-10), now.AddDays(35).Date);

        Add("Cloud Engineer", "Khalti Digital Wallet", JobCategory.SoftwareDevelopment, JobType.FullTime, ExperienceLevel.MidLevel, "Kathmandu", 85000, 125000, true,
            "Design and operate secure, scalable cloud infrastructure for high-traffic payment systems.",
            "Architect cloud solutions on GCP\nManage networking and security\nAutomate deployments\nHandle incident response",
            "3+ years with public clouds\nSecurity-first mindset\nAutomation experience",
            "Degree in CS/IT or equivalent", "GCP, Kubernetes, Terraform, Linux", "Health cover, yearly bonus, wellness program", 15, false, JobStatus.Active, now.AddDays(-14), now.AddDays(28).Date);

        Add("Cyber Security Analyst", "Nepal SBI Bank", JobCategory.Other, JobType.FullTime, ExperienceLevel.MidLevel, "Kathmandu", 70000, 105000, true,
            "Protect the bank's systems and customer data through monitoring, testing and awareness programs.",
            "Monitor security alerts and logs\nConduct vulnerability assessments\nCoordinate incident response\nRun security awareness training",
            "2-4 years in security roles\nSIEM experience\nSecurity certifications preferred",
            "Degree in Cybersecurity, IT or related", "SIEM, Network Security, Penetration Testing", "Bank benefits, medical insurance, provident fund", 30, false, JobStatus.Active, now.AddDays(-7), now.AddDays(38).Date);

        Add("Digital Marketing Specialist", "Yomari Tech", JobCategory.Marketing, JobType.FullTime, ExperienceLevel.Entry, "Lalitpur", 30000, 45000, true,
            "Drive organic and paid growth for our products and raise our employer brand.",
            "Run paid campaigns on Google and Meta\nManage SEO content plan\nReport on marketing KPIs\nSupport employer branding",
            "1-2 years in digital marketing\nGoogle Ads experience\nAnalytical, data-driven mindset",
            "Degree in Marketing or Communications", "SEO, Google Ads, Social Media, GA4", "Learning budget, flexible hours, team retreats", 20, false, JobStatus.Active, now.AddDays(-3), now.AddDays(30).Date);

        Add("HR Officer", "Chandragiri Hills", JobCategory.HumanResources, JobType.FullTime, ExperienceLevel.Entry, "Kathmandu", 28000, 38000, true,
            "Support recruitment, onboarding and employee engagement across our hospitality teams.",
            "Run end-to-end recruitment\nOnboard new employees\nManage HR records and payroll inputs\nPlan engagement activities",
            "1-2 years in HR\nGood communication skills\nDiscretion and professionalism",
            "MBA or degree in HRM", "Recruitment, HRIS, Labour Law basics", "Staff meal, festival bonus, travel perks", 20, true, JobStatus.PendingVerification, now.AddDays(-1), now.AddDays(30).Date);

        Add("Sales Executive", "Daraz Nepal", JobCategory.Sales, JobType.FullTime, ExperienceLevel.Entry, "Kathmandu", 25000, 40000, true,
            "Grow our seller base and help merchants succeed on the marketplace.",
            "Acquire and onboard new sellers\nAdvise sellers on performance\nMeet sales targets\nHandle renewals",
            "Freshers with strong drive welcome\nFluency in Nepali and English\nPersuasive communication",
            "Bachelor's degree in any field", "B2B Sales, Negotiation, CRM", "Sales incentives, staff discounts, transport allowance", 30, false, JobStatus.Active, now.AddDays(-9), now.AddDays(30).Date);

        Add("Customer Support Representative", "Khalti Digital Wallet", JobCategory.Support, JobType.FullTime, ExperienceLevel.Entry, "Kathmandu", 20000, 28000, true,
            "Help customers resolve issues with wallets, payments and merchant services.",
            "Respond to tickets and calls\nResolve payment issues\nEscalate complex cases\nDocument common problems",
            "Strong written and spoken English\nBasic computer skills\nPatience and empathy",
            "+2 or Bachelor's in any field", "Customer Service, CRM, Communication", "Health cover, shift allowance, training", 40, false, JobStatus.Active, now.AddDays(-18), now.AddDays(20).Date);

        Add("Accountant", "Nepal SBI Bank", JobCategory.Finance, JobType.FullTime, ExperienceLevel.MidLevel, "Kathmandu", 45000, 65000, true,
            "Own financial records, reporting and tax compliance for the bank's administrative units.",
            "Prepare financial statements\nEnsure tax compliance\nManage payable/receivable\nSupport audits",
            "3+ years in accounting\nStrong knowledge of Nepali tax law\nAdvanced Excel",
            "MSc Accounting or ICAN track", "Tally, Excel, Taxation, Reporting", "Bank benefits, loan facilities, medical insurance", 30, false, JobStatus.Active, now.AddDays(-6), now.AddDays(40).Date);

        Add("React Developer (Contract)", "Yomari Tech", JobCategory.SoftwareDevelopment, JobType.Contract, ExperienceLevel.Junior, "Remote (Nepal)", 35000, 55000, true,
            "Six-month contract to build internal and client-facing dashboards with React.",
            "Implement UI components\nWrite semantic, accessible HTML\nOptimize performance\nCollaborate with designers",
            "1-2 years with React\nStrong HTML/CSS fundamentals\nAttention to detail",
            "BSc in CS or equivalent", "React, TypeScript, CSS, Tailwind", "Fully remote, flexible hours", 30, true, JobStatus.Rejected, now.AddDays(-2), now.AddDays(25).Date);

        Add("Frontend Developer", "Yomari Tech", JobCategory.SoftwareDevelopment, JobType.Remote, ExperienceLevel.Junior, "Remote (Nepal)", 35000, 55000, true,
            "Build polished, accessible interfaces with React for enterprise products.",
            "Implement UI components\nWrite semantic, accessible HTML\nOptimize performance\nCollaborate with designers",
            "1-2 years with React\nStrong HTML/CSS fundamentals\nAttention to detail",
            "BSc in CS or equivalent", "React, TypeScript, CSS, Tailwind", "Fully remote, flexible hours, learning budget", 30, true, JobStatus.Active, now.AddDays(-22), now.AddDays(20).Date);

        Add("Intern - Data Analytics", "CloudFactory", JobCategory.Data, JobType.Internship, ExperienceLevel.Entry, "Lalitpur", 15000, 20000, true,
            "Three-month paid internship for students interested in a career in data.",
            "Learn analytics tools\nSupport the analytics team\nComplete a mini project\nPresent findings",
            "Final-year students preferred\nBasic SQL knowledge",
            "Currently enrolled in a Bachelor's program", "SQL, Excel, Curiosity", "Stipend, mentorship, certificate", 25, false, JobStatus.Active, now.AddDays(-11), now.AddDays(18).Date);

        if (companies.Count == 0)
            return;

        await context.Jobs.AddRangeAsync(jobs);
        await context.SaveChangesAsync();
    }

    private static async Task SeedApplicationsAsync(ApplicationDbContext context)
    {
        if (await context.Applications.AnyAsync()) return;

        var jobSeekers = await context.JobSeekers.Include(js => js.User).ToListAsync();
        var jobs = await context.Jobs.Where(j => j.Status == JobStatus.Active).ToListAsync();
        var rng = new Random(7);

        var seeker = jobSeekers.FirstOrDefault(s => s.User?.Email == "jobseeker@elevateworkforce.local");
        if (seeker is not null && jobs.Any())
        {
            var targetJobs = jobs.Take(4).ToList();
            var statuses = new[] { ApplicationStatus.Applied, ApplicationStatus.UnderReview, ApplicationStatus.Shortlisted, ApplicationStatus.Interview };
            for (int i = 0; i < targetJobs.Count && i < statuses.Length; i++)
            {
                await context.Applications.AddAsync(new JobApplication
                {
                    Job = targetJobs[i],
                    JobSeeker = seeker,
                    CoverLetter = "I am excited to apply for this role. My background and skills align well with what the team needs, and I am eager to contribute.",
                    Status = statuses[i],
                    CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(2, 15))
                });
            }

            await context.SavedJobs.AddAsync(new SavedJob { JobSeeker = seeker, Job = jobs[rng.Next(jobs.Count)] });
        }

        for (int i = 1; i < jobSeekers.Count; i++)
        {
            var s = jobSeekers[i];
            var count = rng.Next(2, 4);
            foreach (var job in jobs.OrderBy(_ => rng.Next()).Take(count))
            {
                if (await context.Applications.AnyAsync(a => a.JobSeekerId == s.Id && a.JobId == job.Id)) continue;
                var pick = rng.Next(1, 8);
                await context.Applications.AddAsync(new JobApplication
                {
                    Job = job,
                    JobSeeker = s,
                    CoverLetter = pick % 3 == 0 ? "I would welcome the opportunity to discuss how I can contribute to your team." : null,
                    Status = (ApplicationStatus)Math.Min(pick, 4),
                    CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 20))
                });
            }
        }

        await context.SaveChangesAsync();

        foreach (var app in await context.Applications.Include(a => a.JobSeeker).ThenInclude(js => js.User).ToListAsync())
        {
            await context.ApplicationTimelineEntries.AddRangeAsync(new[]
            {
                new JobApplicationTimelineEntry
                {
                    ApplicationId = app.Id, Status = ApplicationStatus.Applied,
                    Note = "Application submitted.", ChangedByName = app.JobSeeker?.User?.FullName ?? "Applicant",
                    CreatedAt = app.CreatedAt
                },
                new JobApplicationTimelineEntry
                {
                    ApplicationId = app.Id, Status = app.Status,
                    Note = $"Application status moved to {app.Status}.", ChangedByName = "System",
                    CreatedAt = app.CreatedAt.AddDays(1)
                }
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureDemoEmployerApplicationsAsync(ApplicationDbContext context)
    {
        var company = await context.Companies.SingleOrDefaultAsync(c => c.Name == DemoCompanyName);
        if (company is null) return;

        var job = await context.Jobs.FirstOrDefaultAsync(j => j.CompanyId == company.Id);
        if (job is null) return;

        if (await context.Applications.AnyAsync(a => a.JobId == job.Id)) return;

        if (job.Status != JobStatus.Active)
        {
            job.Status = JobStatus.Active;
            job.PublishedAt ??= DateTime.UtcNow.AddDays(-3);
            job.Deadline ??= DateTime.UtcNow.AddDays(30);
        }

        var emails = new[] { "anita.gurung@gmail.com", "sunita.rai@gmail.com", "kabita.neupane@gmail.com" };
        var seekers = await context.JobSeekers
            .Where(s => emails.Contains(s.User!.Email!))
            .ToListAsync();
        if (seekers.Count == 0) return;

        var now = DateTime.UtcNow;
        var statuses = new[] { ApplicationStatus.UnderReview, ApplicationStatus.Shortlisted, ApplicationStatus.Interview };

        foreach (var (seeker, index) in seekers.Select((s, i) => (s, i)))
        {
            var app = new JobApplication
            {
                Job = job,
                JobSeeker = seeker,
                CoverLetter = "I am very interested in this role and believe my experience maps well to what the team is looking for. I would welcome the chance to discuss further.",
                Status = statuses[Math.Min(index, statuses.Length - 1)],
                CreatedAt = now.AddDays(-(index + 2))
            };
            await context.Applications.AddAsync(app);
            await context.ApplicationTimelineEntries.AddRangeAsync(new[]
            {
                new JobApplicationTimelineEntry
                {
                    Application = app, Status = ApplicationStatus.Applied,
                    Note = "Application submitted.", ChangedByName = seeker.User?.FullName ?? "Applicant",
                    CreatedAt = app.CreatedAt
                },
                new JobApplicationTimelineEntry
                {
                    Application = app, Status = app.Status,
                    Note = $"Application status moved to {app.Status}.", ChangedByName = "System",
                    CreatedAt = app.CreatedAt.AddDays(1)
                }
            });
        }

        await context.SaveChangesAsync();
    }
}