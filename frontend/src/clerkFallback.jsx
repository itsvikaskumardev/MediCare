import React from "react";

// Mock User object for Frontend Development mode
const mockPatientUser = {
  id: "mock_patient_123",
  fullName: "Demo Patient",
  firstName: "Demo",
  lastName: "Patient",
  primaryEmailAddress: {
    emailAddress: "patient@medicare.dev",
  },
  publicMetadata: {
    role: "patient",
  },
};

export function ClerkProvider({ children }) {
  return <>{children}</>;
}

const userContext = {
  isLoaded: true,
  isSignedIn: true,
  user: mockPatientUser,
};

export function useUser() {
  return userContext;
}

const authContext = {
  isLoaded: true,
  isSignedIn: true,
  userId: mockPatientUser.id,
  getToken: async () => "mock-patient-token-123",
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
      title="Demo Patient (Mock Mode)"
    >
      DP
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
