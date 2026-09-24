using algo.Application.Features.Shop.Wallet.Dtos;
using MediatR;

namespace algo.Application.Features.Shop.Wallet.Queries.GetAdminWalletOverview;

public sealed record GetAdminWalletOverviewQuery : IRequest<AdminWalletOverviewDto>;
