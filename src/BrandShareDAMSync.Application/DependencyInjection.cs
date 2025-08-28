using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using MediatR;



namespace BrandshareDamSync.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg => 
            {
                cfg.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxNzg2NzUyMDAwIiwiaWF0IjoiMTc1NTI4MDAxOCIsImFjY291bnRfaWQiOiIwMTk4YWVkNzMyYTI3NWQ3OGI4MWQ2NWIxM2ViNjYwNCIsImN1c3RvbWVyX2lkIjoiY3RtXzAxazJxZGYyejRoM2Z4d243dms3ZmZrOGVjIiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.m0qDj1CMysKQcb02q3UEukLqQ8BnCuygZcCZaGWSfcfR5iM7sFelAnRRvSrhLn8KKvHi0gPJOsdv_fvCX10UcYc29kta2Q8Qpcp1B1cD8qka6QYMeKI5kPdQT3_fb_J6d_B66HIXOVt2GmVjRtZOreFUWjdUQVOJbcm3-M2l8zp68Wf7o46Bl-ZLD3aO9BiQMXdWpPulm4N1PyEPMwFTCdGUOFNiOFHgtDJMQUUQdT44doaxs6QvA3KTTuBuhAnzJQqU8KMxwZn8lYuMzAXKj3sGIwxVTbawOCaGKM9hhkuWGqnbSCvN71ibSdZrJfBoR8Mw2lOMbuZBE21w8a_imw";
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });
            return services;
        }
    }
}
