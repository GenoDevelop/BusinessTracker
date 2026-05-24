using System.Transactions;

namespace GenoDev.Utilities.Core
{
    /// <summary>
    /// Provides helper methods for managing transaction scopes and executing operations
    /// within a transactional context. Ensures proper management of transactions, including
    /// handling nested transactions and synchronous/asynchronous operations.
    /// </summary>
    public static class TransactionHelper
    {
        private static TransactionScope CreateStandardTransactionScope()
        {
            var transactionOptions = new TransactionOptions 
            {
                IsolationLevel = IsolationLevel.ReadUncommitted,
                Timeout = TransactionManager.MaximumTimeout
            };

            return new TransactionScope(
                TransactionScopeOption.Required,
                transactionOptions,
                TransactionScopeAsyncFlowOption.Enabled);
        }

        /// <summary>
        /// Executes the provided asynchronous operation within a transactional context.
        /// If there is an existing transaction, it executes the operation without creating
        /// a new transaction scope.
        /// </summary>
        /// <param name="action">The asynchronous operation to execute within the transactional context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task Wrap(Func<Task> action)
        {
            if (Transaction.Current != null)
            {
                // Skip creating new transaction scope to avoid external scope
                // being ROLLBACK when some exception causes inner scope to be disposed.
                // This is helpful for our tests and lets test-related transaction be
                // independent of the behavior of the app.
                await action();
                return;
            }

            using var transaction = CreateStandardTransactionScope();
            await action();

            transaction.Complete();
        }

        /// <summary>
        /// Executes the provided asynchronous operation within a transactional context.
        /// If there is an existing transaction, it executes the operation without creating
        /// a new transaction scope.
        /// </summary>
        /// <param name="action">The asynchronous operation to execute within the transactional context.</param>
        /// <typeparam name="TResult">The type of the result returned by the asynchronous operation.</typeparam>
        /// <returns>A task that represents the asynchronous operation and returns the result of type <typeparamref name="TResult"/>.</returns>
        public static async Task<TResult> Wrap<TResult>(Func<Task<TResult>> action)
        {
            if (Transaction.Current != null)
                // Skip creating new transaction scope to avoid external scope
                // being ROLLBACK when some exception causes inner scope to be disposed.
                // This is helpful for our tests and lets test-related transaction be
                // independent of the behavior of the app.
                return await action();

            using var transaction = CreateStandardTransactionScope();
            var result = await action();
                
            transaction.Complete();

            return result;
        }
    }
}