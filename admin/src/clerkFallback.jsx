import React from "react";

// Mock User object for Admin Development mode
const mockAdminUser = {
  id: "mock_admin_123",
  fullName: "Admin Dev",
  firstName: "Admin",
  lastName: "Dev",
  primaryEmailAddress: {
    emailAddress: "admin@medicare.dev",
  },
  publicMetadata: {
    role: "admin",
  },
};

export function ClerkProvider({ children }) {
  return <>{children}</>;
}

const userContext = {
  isLoaded: true,
  isSignedIn: true,
  user: mockAdminUser,
};

export function useUser() {
  return userContext;
}

const authContext = {
  isLoaded: true,
  isSignedIn: true,
  userId: mockAdminUser.id,
  getToken: async () => "mock-admin-token-123",
};

export function useAuth() {
  return authContext;
}

const clerkContext = {
  signOut: () => {
    console.info("[MockClerk] Sign out requested");
  },
  openSignIn: () => {
    console.info("[MockClerk] Open Sign In modal");
  },
  openSignUp: () => {
    console.info("[MockClerk] Open Sign Up modal");
  },
};

export function useClerk() {
  return clerkContext;
}

export function SignedIn({ children }) {
  return <>{children}</>;
}

export function SignedOut({ children }) {
  return null;
}

export function UserButton() {
  return (
    <div
      className="w-8 h-8 rounded-full bg-emerald-600 text-white flex items-center justify-center font-bold text-sm cursor-pointer shadow-sm select-none"
      title="Admin Dev (Mock Mode)"
    >
      AD
    </div>
  );
}

export function SignInButton({ children }) {
  return <>{children || <button>Sign In</button>}</>;
}

export function SignOutButton({ children }) {
  return <>{children || <button>Sign Out</button>}</>;
}

export default {
  ClerkProvider,
  useUser,
  useAuth,
  useClerk,
  SignedIn,
  SignedOut,
  UserButton,
  SignInButton,
  SignOutButton,
};
