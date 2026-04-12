using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;

namespace Shizou.Server.Controllers;

public static class EntityEndpoints
{
    public static Ok<List<TEntity>> GetAll<TEntity>(ShizouDbSet<TEntity> dbSet) where TEntity : class
        => TypedResults.Ok(dbSet.AsNoTracking().ToList());

    public static Results<Ok<TEntity>, NotFound> GetById<TEntity>(
        ShizouDbSet<TEntity> dbSet,
        Expression<Func<TEntity, bool>> predicate) where TEntity : class
    {
        var result = dbSet.AsNoTracking().SingleOrDefault(predicate);
        if (result is null)
            return TypedResults.NotFound();
        return TypedResults.Ok(result);
    }

    public static Results<Created<TEntity>, Conflict> Create<TEntity>(
        ShizouDbSet<TEntity> dbSet,
        IShizouContext context,
        TEntity entity,
        Expression<Func<TEntity, bool>> existsPredicate,
        Func<TEntity, string?> urlFactory) where TEntity : class
    {
        var newEntity = dbSet.Add(entity).Entity;
        try
        {
            context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            if (dbSet.Any(existsPredicate))
                return TypedResults.Conflict();
            throw;
        }

        return TypedResults.Created(urlFactory(newEntity), newEntity);
    }

    public static Results<NoContent, NotFound, ProblemHttpResult> Update<TEntity>(
        ShizouDbSet<TEntity> dbSet,
        IShizouContext context,
        TEntity entity,
        int id,
        Expression<Func<TEntity, bool>> existsPredicate) where TEntity : class
    {
        if (id == 0)
            return TypedResults.Problem("Entity id cannot be 0", title: "Invalid Entity Id",
                statusCode: StatusCodes.Status400BadRequest);
        context.Entry(entity).State = EntityState.Modified;
        try
        {
            context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            if (!dbSet.Any(existsPredicate))
                return TypedResults.NotFound();
            throw;
        }

        return TypedResults.NoContent();
    }

    public static Results<NoContent, NotFound> Remove<TEntity>(
        ShizouDbSet<TEntity> dbSet,
        IShizouContext context,
        Expression<Func<TEntity, bool>> predicate) where TEntity : class
    {
        var entity = dbSet.FirstOrDefault(predicate);
        if (entity is null)
            return TypedResults.NotFound();
        dbSet.Remove(entity);
        context.SaveChanges();
        return TypedResults.NoContent();
    }
}
