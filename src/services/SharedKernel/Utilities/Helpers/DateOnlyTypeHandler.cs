using Dapper;
using System.Data;

namespace SharedKernel.Utilities.Helpers;

/// <summary>
/// Dapper does not map <see cref="DateOnly"/> to SQL parameters by default.
/// </summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        DateTimeOffset dto => DateOnly.FromDateTime(dto.DateTime),
        _ => DateOnly.FromDateTime(Convert.ToDateTime(value))
    };
}
