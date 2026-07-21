using OrcaFacil.Application.DTOs;

namespace OrcaFacil.Application.Admin;

public class AdminService
{
    public AdminDashboardDto EmptyDashboard() => new(0, 0, 0, 0);
}
