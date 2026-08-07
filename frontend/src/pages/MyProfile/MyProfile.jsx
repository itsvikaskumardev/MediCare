import React, { useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import Navbar from "../../components/Navbar/Navbar";
import Footer from "../../components/Footer/Footer";
import {
  Edit2,
  Save,
  X,
  User,
  Image as ImageIcon,
  Check,
  AlertCircle,
  Clock,
  Heart,
  Droplet,
  FileText,
  Shield,
  Phone,
  MapPin,
  Briefcase
} from "lucide-react";
import { editProfilePageStyles } from "../../assets/dummyStyles";

const API_BASE = `${import.meta.env.VITE_BACKEND_URL || "http://localhost:5205"}/api/user/profile`;

export default function MyProfile() {
  const { user: authUser, token } = useAuth();

  const [profile, setProfile] = useState(null);
  const [editing, setEditing] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [toasts, setToasts] = useState([]);

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

  const fetchProfile = async () => {
    try {
      setLoading(true);
      if (!token) return;

      const res = await fetch(API_BASE, {
        headers: { Authorization: `Bearer ${token}` }
      });
      const json = await res.json();
      
      if (!res.ok) throw new Error(json?.errorMessages?.[0] || "Failed to fetch profile");
      
      const d = json.result;
      setProfile(d);
    } catch (err) {
      console.error("fetchProfile error:", err);
      addToast("Unable to load profile", "error");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (authUser && token) {
      fetchProfile();
    }
  }, [authUser, token]);

  const handleSave = async () => {
    try {
      setSaving(true);
      
      const payload = {
        name: profile.name,
        mobile: profile.mobile,
        age: profile.age,
        gender: profile.gender,
        bloodGroup: profile.bloodGroup,
        medicalHistory: profile.medicalHistory,
        allergies: profile.allergies,
        emergencyContactName: profile.emergencyContactName,
        emergencyContactNumber: profile.emergencyContactNumber,
        insuranceProvider: profile.insuranceProvider,
        insurancePolicyNumber: profile.insurancePolicyNumber,
      };

      const res = await fetch(API_BASE, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(payload)
      });
      
      const json = await res.json();
      if (!res.ok) throw new Error(json?.errorMessages?.[0] || "Failed to update profile");
      
      setProfile(json.result);
      setEditing(false);
      addToast("Profile updated successfully!", "success");
    } catch (err) {
      console.error(err);
      addToast(err.message, "error");
    } finally {
      setSaving(false);
    }
  };

  const handleReset = () => {
    fetchProfile();
    setEditing(false);
    addToast("Changes discarded", "info");
  };

  const updateField = (field, value) => {
    setProfile(prev => ({ ...prev, [field]: value }));
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
        <Footer />
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="min-h-screen flex flex-col bg-slate-50">
        <Navbar />
        <div className={styles.loadingContainer}>
          <div className={styles.errorText}>Failed to load profile. Please try again.</div>
        </div>
        <Footer />
      </div>
    );
  }

  // Same fields config as doctor profile but for patient
  const personalFields = [
    { key: "name", label: "Name", type: "text", icon: User },
    { key: "age", label: "Age", type: "number", icon: Clock },
    { key: "gender", label: "Gender", type: "select", icon: User, options: ["Male", "Female", "Other"] },
    { key: "mobile", label: "Mobile", type: "text", icon: Phone },
  ];

  const medicalFields = [
    { key: "bloodGroup", label: "Blood Group", type: "text", icon: Droplet },
    { key: "insuranceProvider", label: "Insurance Provider", type: "text", icon: Shield },
    { key: "insurancePolicyNumber", label: "Policy Number", type: "text", icon: FileText },
  ];

  const emergencyFields = [
    { key: "emergencyContactName", label: "Contact Name", type: "text", icon: Heart },
    { key: "emergencyContactNumber", label: "Contact Number", type: "text", icon: Phone },
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
                    src={profile.imageUrl || authUser?.imageUrl || "https://ui-avatars.com/api/?name=Patient&background=random"}
                    alt="Profile"
                    className={styles.profileImage}
                    onError={(e) => {
                      e.currentTarget.onerror = null;
                      if (!e.currentTarget.src.includes("ui-avatars.com")) {
                        e.currentTarget.src = "https://ui-avatars.com/api/?name=Patient&background=random";
                      }
                    }}
                  />
                  <label className={styles.imageEditButton(editing)}>
                    {/* Placeholder for future image upload */}
                    <input
                      type="file"
                      accept="image/*"
                      className={styles.imageInput}
                      disabled={!editing}
                    />
                    <ImageIcon className={styles.imageEditIcon(editing)} />
                  </label>
                </div>
              </div>
            </div>

            <div className={styles.profileContent}>
              <div className={styles.profileHeader}>
                <div className={styles.profileInfo}>
                  <h1 className={styles.profileName}>{profile.name || "Patient"}</h1>
                  <p className={styles.profileSubtitle}>
                    <User className={styles.subtitleIcon} />
                    <span className="truncate">Patient Profile</span>
                  </p>

                  <div className={styles.statsContainer}>
                    {/* Age Stat */}
                    <div className={styles.statItem}>
                      <Clock
                        className={`${styles.statIcon} ${styles.statEmeraldIcon}`}
                      />
                      <div className="flex items-center gap-3">
                        <div className="flex flex-col">
                          <div className={styles.statLabel}>Age</div>
                          {!editing ? (
                            <div className={styles.statValue}>{profile.age || "-"}</div>
                          ) : (
                            <input
                              type="number"
                              value={profile.age || ""}
                              onChange={(e) => updateField("age", e.target.value ? Number(e.target.value) : null)}
                              className={styles.statPatientsInput}
                              placeholder="Age"
                            />
                          )}
                        </div>
                      </div>
                    </div>

                    {/* Blood Group Stat */}
                    <div className={styles.statItem}>
                      <Droplet
                        className={`${styles.statIcon} ${styles.statEmeraldIcon}`}
                      />
                      <div className="flex items-center gap-3">
                        <div className="flex flex-col">
                          <div className={styles.statLabel}>Blood Group</div>
                          {!editing ? (
                            <div className={styles.statValue}>{profile.bloodGroup || "-"}</div>
                          ) : (
                            <input
                              type="text"
                              value={profile.bloodGroup || ""}
                              onChange={(e) => updateField("bloodGroup", e.target.value)}
                              className={styles.statPatientsInput}
                              placeholder="e.g. O+"
                            />
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div className={styles.actionButtons}>
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
                <h2 className={styles.sectionTitle}>Personal Information</h2>
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
                    {field.type === "select" ? (
                      <select
                        value={profile[field.key] || ""}
                        onChange={(e) => updateField(field.key, e.target.value)}
                        className={styles.inputBase(editing)}
                        disabled={!editing}
                      >
                        <option value="">Select {field.label}</option>
                        {field.options.map(opt => (
                          <option key={opt} value={opt}>{opt}</option>
                        ))}
                      </select>
                    ) : (
                      <input
                        type={field.type}
                        value={profile[field.key] || ""}
                        onChange={(e) => updateField(field.key, field.type === 'number' ? Number(e.target.value) : e.target.value)}
                        className={styles.inputBase(editing)}
                        placeholder={`Enter ${field.label}`}
                        readOnly={!editing}
                      />
                    )}
                  </div>
                ))}
              </div>

              {/* Emergency Contact */}
              <div className={styles.sectionDivider} />
              <div className={styles.sectionHeader}>
                <div className={styles.sectionIconContainer}>
                  <Heart className={styles.sectionIcon} />
                </div>
                <h2 className={styles.sectionTitle}>Emergency Contact</h2>
              </div>
              <div className={styles.fieldGrid}>
                {emergencyFields.map((field) => (
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
                      className={styles.inputBase(editing)}
                      placeholder={`Enter ${field.label}`}
                      readOnly={!editing}
                    />
                  </div>
                ))}
              </div>

              {/* Medical Information */}
              <div className={styles.sectionDivider} />
              <div className={styles.sectionHeader}>
                <div className={styles.sectionIconContainer}>
                  <FileText className={styles.sectionIcon} />
                </div>
                <h2 className={styles.sectionTitle}>Medical Information</h2>
              </div>
              
              <div className={styles.fieldGrid}>
                {/* Medical History */}
                <div className={styles.fieldGroup}>
                  <div className={styles.fieldHeader}>
                    <div className={styles.fieldIconContainer(editing)}>
                      <FileText className={styles.fieldIcon} />
                    </div>
                    <label className={styles.fieldLabel}>Medical History</label>
                  </div>
                  <input
                    value={profile.medicalHistory || ""}
                    onChange={(e) => updateField("medicalHistory", e.target.value)}
                    className={styles.inputBase(editing)}
                    placeholder="Past surgeries, chronic conditions, etc."
                    readOnly={!editing}
                  />
                </div>

                {/* Allergies */}
                <div className={styles.fieldGroup}>
                  <div className={styles.fieldHeader}>
                    <div className={styles.fieldIconContainer(editing)}>
                      <AlertCircle className={styles.fieldIcon} />
                    </div>
                    <label className={styles.fieldLabel}>Allergies</label>
                  </div>
                  <input
                    value={profile.allergies || ""}
                    onChange={(e) => updateField("allergies", e.target.value)}
                    className={styles.inputBase(editing)}
                    placeholder="Food, drug or environmental allergies"
                    readOnly={!editing}
                  />
                </div>
              </div>

              {/* Insurance Details */}
              <div className={styles.sectionDivider} />
              <div className={styles.sectionHeader}>
                <div className={styles.sectionIconContainer}>
                  <Shield className={styles.sectionIcon} />
                </div>
                <h2 className={styles.sectionTitle}>Insurance Details</h2>
              </div>
              <div className={styles.fieldGrid}>
                {medicalFields.slice(1).map((field) => ( // skip blood group here as it is in stats
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
                      className={styles.inputBase(editing)}
                      placeholder={`Enter ${field.label}`}
                      readOnly={!editing}
                    />
                  </div>
                ))}
              </div>

            </div>
          </div>
        </div>
      </div>
      
      <Footer />
    </div>
  );
}
