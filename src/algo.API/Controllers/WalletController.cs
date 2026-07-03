using algo.Application.Common.Identity;
using algo.Application.Features.Shop.Wallet.Commands.DeleteUserWalletTransactions;
using algo.Application.Features.Shop.Wallet.Commands.ChargeWallet;
using algo.Application.Features.Shop.Wallet.Commands.FreezeWalletFunds;
using algo.Application.Features.Shop.Wallet.Commands.StopUserWallet;
using algo.Application.Features.Shop.Wallet.Commands.UnfreezeWalletFunds;
using algo.Application.Features.Shop.Wallet.Commands.WithdrawWalletFunds;
using algo.Application.Features.Shop.Wallet.Dtos;
using algo.Application.Features.Shop.Wallet.Queries.GetAdminWalletOverview;
using algo.Application.Features.Shop.Wallet.Queries.GetMyWalletBalance;
using algo.Application.Features.Shop.Wallet.Queries.GetWalletTransactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[Authorize]
public sealed class WalletController(IMediator mediator) : BaseController(mediator)
{
    public sealed record StopWalletRequest(string? Description);

    [HttpGet("balance")]
    [ProducesResponseType(typeof(IReadOnlyList<WalletBalanceDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<WalletBalanceDto>> Balance(CancellationToken cancellationToken) =>
        mediator.Send(new GetMyWalletBalanceQuery(), cancellationToken);

    [HttpPost("charge")]
    [ProducesResponseType(typeof(WalletTransactionDto), StatusCodes.Status200OK)]
    public Task<WalletTransactionDto> Charge(
        [FromBody] ChargeWalletCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPost("withdraw")]
    [ProducesResponseType(typeof(WalletTransactionDto), StatusCodes.Status200OK)]
    public Task<WalletTransactionDto> Withdraw(
        [FromBody] WithdrawWalletFundsCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPost("freeze")]
    [ProducesResponseType(typeof(WalletTransactionDto), StatusCodes.Status200OK)]
    public Task<WalletTransactionDto> Freeze(
        [FromBody] FreezeWalletFundsCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPost("unfreeze")]
    [ProducesResponseType(typeof(WalletTransactionDto), StatusCodes.Status200OK)]
    public Task<WalletTransactionDto> Unfreeze(
        [FromBody] UnfreezeWalletFundsCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpGet("transactions")]
    [ProducesResponseType(typeof(IReadOnlyList<WalletTransactionDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<WalletTransactionDto>> Transactions(CancellationToken cancellationToken) =>
        mediator.Send(new GetWalletTransactionsQuery(), cancellationToken);

    [Authorize(Roles = DefaultRoles.Admin)]
    [HttpGet("admin/overview")]
    [ProducesResponseType(typeof(AdminWalletOverviewDto), StatusCodes.Status200OK)]
    public Task<AdminWalletOverviewDto> AdminOverview(CancellationToken cancellationToken) =>
        mediator.Send(new GetAdminWalletOverviewQuery(), cancellationToken);

    [Authorize(Roles = DefaultRoles.Admin)]
    [HttpPatch("admin/users/{userId}/stop")]
    [ProducesResponseType(typeof(IReadOnlyList<WalletTransactionDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<WalletTransactionDto>> StopUserWallet(
        string userId,
        [FromBody] StopWalletRequest? request,
        CancellationToken cancellationToken) =>
        mediator.Send(new StopUserWalletCommand(userId, request?.Description), cancellationToken);

    [Authorize(Roles = DefaultRoles.Admin)]
    [HttpDelete("admin/users/{userId}/transactions")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public Task<int> DeleteUserWalletTransactions(string userId, CancellationToken cancellationToken) =>
        mediator.Send(new DeleteUserWalletTransactionsCommand(userId), cancellationToken);
}
