using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Attendance;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Shared.Security;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Attendance
{
    /// <summary>
    /// 考勤会话端点映射
    /// </summary>
    public static class AttendanceEndpoints
    {
        /// <summary>
        /// 映射考勤会话相关端点
        /// </summary>
        /// <param name="group">路由组</param>
        public static RouteGroupBuilder MapAttendanceEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/sessions", async (SessionCreateDto dto, IMediator mediator, ICurrentUser currentUser) =>
            {
                var result = await mediator.Send(new CreateSessionCommand(dto, currentUser.UserId));
                return Results.Ok(result);
            })
            .WithName("CreateSession").WithSummary("创建考勤会话").RequireAuthorization("RequireTeacher")
            .Produces<ApiResponse<SessionResponseDto>>(StatusCodes.Status200OK);

            group.MapGet("/sessions/{id:long}", async (long id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetSessionByIdQuery(id));
                return Results.Ok(result);
            })
            .WithName("GetSessionById").WithSummary("查询会话详情").RequireAuthorization()
            .Produces<ApiResponse<SessionResponseDto>>(StatusCodes.Status200OK);

            group.MapGet("/sessions/active", async (IMediator mediator, ICurrentUser currentUser) =>
            {
                var result = await mediator.Send(new GetActiveSessionsQuery(currentUser.UserId));
                return Results.Ok(result);
            })
            .WithName("GetActiveSessions").WithSummary("查询教师进行中的会话").RequireAuthorization("RequireTeacher")
            .Produces<ApiResponse<List<SessionResponseDto>>>(StatusCodes.Status200OK);

            group.MapGet("/sessions/history", async ([AsParameters] GetSessionsByTeacherQuery query, IMediator mediator, ICurrentUser currentUser) =>
            {
                var actualQuery = query with { TeacherId = currentUser.UserId };
                var result = await mediator.Send(actualQuery);
                return Results.Ok(result);
            })
            .WithName("GetSessionsByTeacher").WithSummary("分页查询教师历史会话").RequireAuthorization("RequireTeacher")
            .Produces<ApiResponse<PagedResult<SessionResponseDto>>>(StatusCodes.Status200OK);

            group.MapPost("/sessions/{id:long}/close", async (long id, IMediator mediator, ICurrentUser currentUser) =>
            {
                var result = await mediator.Send(new CloseSessionCommand(id, currentUser.UserId));
                return Results.Ok(result);
            })
            .WithName("CloseSession").WithSummary("关闭会话").RequireAuthorization("RequireTeacher")
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

            group.MapGet("/sessions/{id:long}/records", async (long id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetSessionRecordsQuery(id));
                return Results.Ok(result);
            })
            .WithName("GetSessionRecords").WithSummary("查询会话签到记录").RequireAuthorization()
            .Produces<ApiResponse<List<AttendanceRecordResponseDto>>>(StatusCodes.Status200OK);

            group.MapPost("/sessions/{id:long}/qrcode", async (long id, IMediator mediator, ICurrentUser currentUser) =>
            {
                var result = await mediator.Send(new GenerateQrCodeCommand(id, currentUser.UserId));
                return Results.Ok(result);
            })
            .WithName("GenerateQrCode").WithSummary("生成二维码").RequireAuthorization("RequireTeacher")
            .Produces<ApiResponse<QrCodeResult>>(StatusCodes.Status200OK);

            group.MapPost("/sessions/{id:long}/checkin", async (long id, string token, IMediator mediator, ICurrentUser currentUser) =>
            {
                var result = await mediator.Send(new CheckInCommand(id, token, currentUser.UserId));
                return Results.Ok(result);
            })
            .WithName("CheckIn").WithSummary("学生扫码签到").RequireAuthorization("RequireStudent")
            .Produces<ApiResponse<CheckInResult>>(StatusCodes.Status200OK);

            group.MapPost("/sessions/{id:long}/rollcall", async (long id, IMediator mediator, ICurrentUser currentUser) =>
            {
                var result = await mediator.Send(new RollCallCommand(id, currentUser.UserId));
                return Results.Ok(result);
            })
            .WithName("RollCall").WithSummary("一键点名").RequireAuthorization("RequireTeacher")
            .Produces<ApiResponse<int>>(StatusCodes.Status200OK);

            group.MapPut("/records/{id:long}/status", async (long id, UpdateRecordStatusDto dto, IMediator mediator, ICurrentUser currentUser) =>
            {
                var result = await mediator.Send(new UpdateRecordStatusCommand(id, dto.Status, currentUser.UserId));
                return Results.Ok(result);
            })
            .WithName("UpdateRecordStatus").WithSummary("修改考勤记录状态").RequireAuthorization("RequireTeacher")
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

            group.MapPost("/sessions/{id:long}/manual-checkin", async (long id, ManualCheckInDto dto, IMediator mediator, ICurrentUser currentUser) =>
            {
                var result = await mediator.Send(new ManualCheckInCommand(id, dto.StudentId, dto.Status, currentUser.UserId));
                return Results.Ok(result);
            })
            .WithName("ManualCheckIn").WithSummary("手动补签").RequireAuthorization("RequireTeacher")
            .Produces<ApiResponse<AttendanceRecordResponseDto>>(StatusCodes.Status200OK);

            group.MapGet("/classes/{classId:long}/random-pick", async (long classId, long? sessionId, IMediator mediator) =>
            {
                var result = await mediator.Send(new RandomPickQuery(classId, sessionId));
                return Results.Ok(result);
            })
            .WithName("RandomPick").WithSummary("随机点名").RequireAuthorization("RequireTeacher")
            .Produces<ApiResponse<RandomPickResult>>(StatusCodes.Status200OK);

            group.MapPost("/sessions/{id:long}/random-pick/mark", async (long id, MarkRandomPickDto dto, IMediator mediator) =>
            {
                var result = await mediator.Send(new MarkRandomPickCommand(id, dto.StudentId, dto.Answered));
                return Results.Ok(result);
            })
            .WithName("MarkRandomPick").WithSummary("标记随机点名结果").RequireAuthorization("RequireTeacher")
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

            return group;
        }
    }
}