import React, { useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import Navbar from "../../components/Navbar/Navbar";
import {
  User,
  Check,
  AlertCircle,
  Briefcase,
  Mail,
  Shield,
  Edit2,
  X,
  Save,
  UserPlus,
  Loader
} from "lucide-react";
import { editProfilePageStyles } from "../../assets/dummyStyles";

export default function AdminProfile() {
  const { user: authUser } = useAuth();
  
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [toasts, setToasts] = useState([]);
  
  const [showCreateAdmin, setShowCreateAdmin] = useState(false);
  const [newAdmin, setNewAdmin] = useState({ name: "", email: "", password: "" });
  const [creatingAdmin, setCreatingAdmin] = useState(false);

  const styles = editProfilePageStyles;

  const addToast = (text, type = "success") => {
    const idt = Date.now() + Math.random();
    const t = { id: idt, text, type };
    setToasts((prev) => [t, ...prev.slice(0, 2)]);
    setTimeout(
      () => setToasts((prev) => prev.filter((it) => it.id !== idt)),
      3000,
    );
  };

  useEffect(() => {
    let mounted = true;
    async function loadProfile() {
      if (!authUser || !authUser.id) return;
      try {
        const token = localStorage.getItem("authToken");
        const API_BASE = import.meta.env.VITE_BACKEND_URL || "http://localhost:5205";
        const res = await fetch(`${API_BASE}/api/user/admin-profile`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (!res.ok) throw new Error("Failed to load profile");
        const body = await res.json();
        const data = body.result || {};
        
        if (mounted) {
          setProfile({
            name: data.name || authUser.fullName || "Administrator",
            email: data.email || authUser.email || "admin@medicare.com",
            role: data.role || authUser.role || "ADMIN",
            id: data.id || authUser.id,
            imageUrl: data.imageUrl || "",
          });
          setLoading(false);
        }
      } catch (err) {
        console.error("Error loading admin profile:", err);
        if (mounted) {
          // Fallback to authUser if fetch fails
          setProfile({
            name: authUser.fullName || "Administrator",
            email: authUser.email || "admin@medicare.com",
            role: authUser.role || "ADMIN",
            id: authUser.id,
            imageUrl: "",
          });
          setLoading(false);
        }
      }
    }
    loadProfile();
    return () => { mounted = false; };
  }, [authUser]);

  const updateField = (field, value) => {
    setProfile(prev => ({ ...prev, [field]: value }));
  };

  const handleReset = () => {
    setProfile({
      name: authUser.fullName || "Administrator",
      email: authUser.email || "admin@medicare.com",
      role: authUser.role || "ADMIN",
      id: authUser.id,
      imageUrl: "",
    });
    setEditing(false);
    addToast("Changes discarded", "info");
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      const token = localStorage.getItem("authToken");
      const API_BASE = import.meta.env.VITE_BACKEND_URL || "http://localhost:5205";
      const response = await fetch(`${API_BASE}/api/user/admin-profile`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          name: profile.name,
          email: profile.email,
          imageUrl: profile.imageUrl
        }),
      });

      if (!response.ok) {
        throw new Error("Failed to update profile");
      }
      
      setEditing(false);
      addToast("Profile updated successfully!", "success");
    } catch (err) {
      console.error(err);
      addToast("Failed to update profile", "error");
    } finally {
      setSaving(false);
    }
  };

  const handleCreateAdmin = async (e) => {
    e.preventDefault();
    setCreatingAdmin(true);
    try {
      const API_BASE = import.meta.env.VITE_BACKEND_URL || "http://localhost:5205";
      const response = await fetch(`${API_BASE}/api/auth/register-admin`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newAdmin)
      });
      const data = await response.json();
      if (response.ok && data.isSuccess) {
        addToast("Admin created successfully!", "success");
        setShowCreateAdmin(false);
        setNewAdmin({ name: "", email: "", password: "" });
      } else {
        addToast(data.errorMessages?.[0] || "Failed to create admin", "error");
      }
    } catch (error) {
      addToast("Network error occurred", "error");
    } finally {
      setCreatingAdmin(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen flex flex-col bg-slate-50">
        <Navbar />
        <div className={styles.loadingContainer}>
          <div className="text-center">
            <div className={styles.loadingSpinner} />
            <div className={styles.loadingText}>Loading profile...</div>
          </div>
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="min-h-screen flex flex-col bg-slate-50">
        <Navbar />
        <div className={styles.loadingContainer}>
          <div className={styles.errorText}>Failed to load profile.</div>
        </div>
      </div>
    );
  }

  const personalFields = [
    { key: "name", label: "Full Name", type: "text", icon: User },
    { key: "email", label: "Email Address", type: "email", icon: Mail },
    { key: "role", label: "System Role", type: "text", icon: Shield },
    { key: "id", label: "Admin ID", type: "text", icon: Briefcase },
  ];

  return (
    <div className="min-h-screen flex flex-col bg-slate-50">
      <Navbar />

      <div className={styles.pageContainer}>
        <div className={styles.maxWidthContainer}>
          {/* Toasts */}
          <div className={styles.toastContainer}>
            {toasts.map((t) => (
              <div
                key={t.id}
                className={`${styles.toastBase} ${t.type === "error"
                  ? styles.toastError
                  : t.type === "info"
                    ? styles.toastInfo
                    : styles.toastSuccess
                  }`}
              >
                {t.type === "error" ? (
                  <AlertCircle
                    className={`${styles.toastIcon} ${styles.toastErrorIcon}`}
                  />
                ) : (
                  <Check
                    className={`${styles.toastIcon} ${styles.toastSuccessIcon}`}
                  />
                )}
                <span className={styles.toastText}>{t.text}</span>
              </div>
            ))}
          </div>

          <div className={styles.mainCard}>
            {/* Header Banner */}
            <div className={styles.headerBackground}>
              <div className={styles.imageContainer}>
                <div className={styles.imageWrapper}>
                  <img
                    src={profile.imageUrl || "https://ui-avatars.com/api/?name=Admin&background=random"}
                    alt="Profile"
                    className={styles.profileImage}
                  />
                </div>
              </div>
            </div>

            <div className={styles.profileContent}>
              <div className={styles.profileHeader}>
                <div className={styles.profileInfo}>
                  <h1 className={styles.profileName}>{profile.name}</h1>
                  <p className={styles.profileSubtitle}>
                    <Shield className={styles.subtitleIcon} />
                    <span className="truncate">System Administrator</span>
                  </p>

                  <div className={styles.statsContainer}>
                    <div className={styles.statItem}>
                      <Briefcase
                        className={`${styles.statIcon} ${styles.statEmeraldIcon}`}
                      />
                      <div className="flex items-center gap-3">
                        <div className="flex flex-col">
                          <div className={styles.statLabel}>Access Level</div>
                          <div className={styles.statValue}>Full Access</div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div className={styles.actionButtons}>
                  <div className="flex flex-col sm:flex-row gap-3">
                    <button
                      onClick={() => {
                        if (editing) handleReset();
                        else setEditing(true);
                      }}
                      className={styles.editButton}
                    >
                      <div className={styles.editButtonContent}>
                        {editing ? <X className="w-4 h-4" /> : <Edit2 className="w-4 h-4" />}
                        <span className="font-medium">
                          {editing ? "Cancel" : "Edit Profile"}
                        </span>
                      </div>
                    </button>

                    {!editing && (
                      <button
                        onClick={() => setShowCreateAdmin(true)}
                        className="group relative overflow-hidden bg-linear-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white px-5 py-2 rounded-full cursor-pointer shadow-lg transition-all duration-300 hover:shadow-xl hover:scale-[1.02] w-full sm:w-auto"
                      >
                        <div className="relative flex items-center gap-2">
                          <UserPlus className="w-4 h-4" />
                          <span className="font-medium">Create Admin</span>
                        </div>
                      </button>
                    )}
                  </div>

                  {editing && (
                    <button
                      onClick={handleSave}
                      className={styles.saveButton || styles.editButton}
                      disabled={saving}
                    >
                      <div className={styles.saveButtonContent || styles.editButtonContent}>
                        {saving ? (
                          <div className={styles.saveSpinner} />
                        ) : (
                          <Save className="w-4 h-4" />
                        )}
                        <span className="font-medium">
                          {saving ? "Saving..." : "Save Profile"}
                        </span>
                      </div>
                    </button>
                  )}
                </div>
              </div>

              {/* Personal Information */}
              <div className={styles.sectionDivider} />
              <div className={styles.sectionHeader}>
                <div className={styles.sectionIconContainer}>
                  <User className={styles.sectionIcon} />
                </div>
                <h2 className={styles.sectionTitle}>Administrator Details</h2>
              </div>
              <div className={styles.fieldGrid}>
                {personalFields.map((field) => (
                  <div key={field.key} className={styles.fieldGroup}>
                    <div className={styles.fieldHeader}>
                      <div className={styles.fieldIconContainer(editing)}>
                        <field.icon className={styles.fieldIcon} />
                      </div>
                      <label className={styles.fieldLabel}>{field.label}</label>
                    </div>
                    <input
                      type={field.type}
                      value={profile[field.key] || ""}
                      onChange={(e) => updateField(field.key, e.target.value)}
                      className={styles.inputBase(editing && field.key !== 'role' && field.key !== 'id')}
                      placeholder={`Enter ${field.label}`}
                      readOnly={!editing || field.key === 'role' || field.key === 'id'}
                    />
                  </div>
                ))}
              </div>

            </div>
          </div>
        </div>
      </div>

      {/* Create Admin Modal */}
      {showCreateAdmin && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden animate-slideIn">
            <div className="bg-linear-to-r from-blue-600 to-blue-700 p-4 flex justify-between items-center text-white">
              <h2 className="text-xl font-bold flex items-center gap-2">
                <UserPlus className="w-5 h-5" />
                Create New Admin
              </h2>
              <button 
                onClick={() => setShowCreateAdmin(false)}
                className="p-1 hover:bg-white/20 rounded-full transition-colors cursor-pointer"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            
            <form onSubmit={handleCreateAdmin} className="p-6">
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Full Name</label>
                  <input
                    type="text"
                    required
                    value={newAdmin.name}
                    onChange={(e) => setNewAdmin(prev => ({ ...prev, name: e.target.value }))}
                    className="w-full rounded-xl border-2 border-gray-200 px-4 py-2 focus:border-blue-500 focus:ring-2 focus:ring-blue-200 outline-none transition-all"
                    placeholder="Admin Name"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Email Address</label>
                  <input
                    type="email"
                    required
                    value={newAdmin.email}
                    onChange={(e) => setNewAdmin(prev => ({ ...prev, email: e.target.value }))}
                    className="w-full rounded-xl border-2 border-gray-200 px-4 py-2 focus:border-blue-500 focus:ring-2 focus:ring-blue-200 outline-none transition-all"
                    placeholder="admin@medicare.com"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Password</label>
                  <input
                    type="password"
                    required
                    minLength={6}
                    value={newAdmin.password}
                    onChange={(e) => setNewAdmin(prev => ({ ...prev, password: e.target.value }))}
                    className="w-full rounded-xl border-2 border-gray-200 px-4 py-2 focus:border-blue-500 focus:ring-2 focus:ring-blue-200 outline-none transition-all"
                    placeholder="••••••••"
                  />
                </div>
              </div>
              
              <div className="mt-6 flex justify-end gap-3">
                <button
                  type="button"
                  onClick={() => setShowCreateAdmin(false)}
                  className="px-5 py-2 rounded-full border border-gray-300 text-gray-700 hover:bg-gray-50 transition-colors font-medium cursor-pointer"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={creatingAdmin}
                  className="px-5 py-2 rounded-full bg-blue-600 hover:bg-blue-700 text-white transition-colors font-medium disabled:opacity-70 disabled:cursor-not-allowed flex items-center gap-2 cursor-pointer"
                >
                  {creatingAdmin ? (
                    <>
                      <Loader className="w-4 h-4 animate-spin" />
                      Creating...
                    </>
                  ) : (
                    "Create Admin"
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
