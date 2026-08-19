import React, { useState, useEffect } from "react";
import { Loader2 } from "lucide-react";

const ServerWakeupLoader = ({ children }) => {
  const [isAwake, setIsAwake] = useState(false);
  const [showLoader, setShowLoader] = useState(false);

  useEffect(() => {
    let isMounted = true;
    const API_BASE = import.meta.env.VITE_BACKEND_URL || import.meta.env.BACKEND_URL || "http://localhost:5205";

    // Show the loader only if it takes more than 500ms
    const loaderTimeout = setTimeout(() => {
      if (isMounted && !isAwake) {
        setShowLoader(true);
      }
    }, 500);

    const checkHealth = async () => {
      while (true) {
        try {
          const res = await fetch(`${API_BASE}/health`, {
            method: "GET",
            headers: {
              "Cache-Control": "no-cache",
            },
          });

          if (res.ok) {
            if (isMounted) {
              setIsAwake(true);
              clearTimeout(loaderTimeout);
            }
            break;
          }
        } catch (error) {
          // Server is likely still sleeping, wait and retry
        }

        // Wait 2 seconds before checking again
        await new Promise((resolve) => setTimeout(resolve, 2000));
      }
    };

    checkHealth();

    return () => {
      isMounted = false;
      clearTimeout(loaderTimeout);
    };
  }, []); // Run once on mount

  if (isAwake || !showLoader) {
    return <>{children}</>;
  }

  return (
    <div className="fixed inset-0 z-50 flex flex-col items-center justify-center bg-white/95 backdrop-blur-sm transition-opacity duration-500">
      <Loader2 className="w-12 h-12 text-emerald-600 animate-spin mb-4" />
      <h2 className="text-2xl font-bold text-gray-800 mb-2">Waking up server...</h2>
      <p className="text-gray-500 text-center max-w-md px-4">
        Please wait a moment while it boots up.
      </p>
    </div>
  );
};

export default ServerWakeupLoader;
