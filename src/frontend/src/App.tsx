import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { FeedPage } from './pages/FeedPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 2,
    },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <FeedPage />
    </QueryClientProvider>
  );
}

export default App;
