# License Verification Integration Guide

## 🔑 Overview

This guide covers license verification functionality including license submission, verification status tracking, document upload, and admin/staff approval workflows for the EV Co-Ownership platform.

## 🏗️ License API Structure

License endpoints are shared across roles:
- **Base URL**: `/api/shared/license`
- **Authentication**: Required for some endpoints
- **File Upload**: Supports multipart/form-data

## 📄 License Verification Process

### 1. Submit License for Verification

**Endpoint**: `POST /api/shared/license/verify`

```typescript
interface VerifyLicenseRequest {
  licenseNumber: string;
  issuedBy: string;
  issueDate: string; // YYYY-MM-DD format
  expiryDate?: string; // YYYY-MM-DD format (optional)
  firstName: string;
  lastName: string;
  dateOfBirth: string; // YYYY-MM-DD format
  licenseImage: File; // Image file of the license
}

interface LicenseVerificationResponse {
  verificationId: number;
  licenseNumber: string;
  status: 'pending' | 'verified' | 'rejected';
  message: string;
  submittedAt: string;
  estimatedProcessingTime: string;
}

export const licenseService = {
  async verifyLicense(licenseData: VerifyLicenseRequest): Promise<BaseResponse<LicenseVerificationResponse>> {
    const formData = new FormData();
    
    // Append all fields to FormData
    formData.append('licenseNumber', licenseData.licenseNumber);
    formData.append('issuedBy', licenseData.issuedBy);
    formData.append('issueDate', licenseData.issueDate);
    formData.append('firstName', licenseData.firstName);
    formData.append('lastName', licenseData.lastName);
    formData.append('dateOfBirth', licenseData.dateOfBirth);
    formData.append('licenseImage', licenseData.licenseImage);
    
    if (licenseData.expiryDate) {
      formData.append('expiryDate', licenseData.expiryDate);
    }

    return await apiClient.post('/shared/license/verify', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  },

  async checkLicenseExists(licenseNumber: string): Promise<BaseResponse<{ exists: boolean }>> {
    return await apiClient.get(`/shared/license/check-exists?licenseNumber=${licenseNumber}`);
  },

  async getLicenseInfo(licenseNumber: string): Promise<BaseResponse<LicenseInfo>> {
    return await apiClient.get(`/shared/license/info?licenseNumber=${licenseNumber}`);
  }
};

// License verification form component
const LicenseVerificationForm: React.FC = () => {
  const [formData, setFormData] = useState<VerifyLicenseRequest>({
    licenseNumber: '',
    issuedBy: '',
    issueDate: '',
    expiryDate: '',
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    licenseImage: null as any
  });
  
  const [previewImage, setPreviewImage] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [checkingExists, setCheckingExists] = useState(false);
  const [licenseExists, setLicenseExists] = useState<boolean | null>(null);

  // Check if license already exists when license number changes
  const checkLicenseExists = async (licenseNumber: string) => {
    if (licenseNumber.length < 8) {
      setLicenseExists(null);
      return;
    }

    setCheckingExists(true);
    try {
      const response = await licenseService.checkLicenseExists(licenseNumber);
      setLicenseExists(response.data.exists);
    } catch (error) {
      console.error('Error checking license:', error);
    } finally {
      setCheckingExists(false);
    }
  };

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      if (formData.licenseNumber) {
        checkLicenseExists(formData.licenseNumber);
      }
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [formData.licenseNumber]);

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      // Validate file size (max 5MB)
      if (file.size > 5 * 1024 * 1024) {
        toast.error('Image size must be less than 5MB');
        return;
      }

      // Validate file type
      if (!file.type.startsWith('image/')) {
        toast.error('Please select a valid image file');
        return;
      }

      setFormData({ ...formData, licenseImage: file });
      
      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        setPreviewImage(e.target?.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (licenseExists) {
      toast.error('This license number is already registered');
      return;
    }

    if (!formData.licenseImage) {
      toast.error('Please upload a license image');
      return;
    }

    setIsSubmitting(true);
    try {
      const response = await licenseService.verifyLicense(formData);
      
      if (response.statusCode === 200) {
        toast.success('License submitted for verification successfully!');
        // Navigate to verification status page
        navigate(`/license/status/${response.data.verificationId}`);
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to submit license';
      toast.error(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="license-verification-form">
      <div className="form-header">
        <h2>Driving License Verification</h2>
        <p>Please provide your driving license information for verification</p>
      </div>

      <form onSubmit={handleSubmit} className="license-form">
        {/* License Number */}
        <div className="form-group">
          <label>License Number *</label>
          <div className="input-with-validation">
            <input
              type="text"
              value={formData.licenseNumber}
              onChange={(e) => setFormData({
                ...formData,
                licenseNumber: e.target.value.toUpperCase()
              })}
              placeholder="Enter your license number"
              required
              className={licenseExists === true ? 'error' : licenseExists === false ? 'success' : ''}
            />
            
            {checkingExists && <span className="checking">Checking...</span>}
            {licenseExists === true && (
              <span className="validation-error">License already registered</span>
            )}
            {licenseExists === false && (
              <span className="validation-success">License available</span>
            )}
          </div>
        </div>

        {/* Personal Information */}
        <div className="form-row">
          <div className="form-group">
            <label>First Name *</label>
            <input
              type="text"
              value={formData.firstName}
              onChange={(e) => setFormData({
                ...formData,
                firstName: e.target.value
              })}
              placeholder="First name as on license"
              required
            />
          </div>
          
          <div className="form-group">
            <label>Last Name *</label>
            <input
              type="text"
              value={formData.lastName}
              onChange={(e) => setFormData({
                ...formData,
                lastName: e.target.value
              })}
              placeholder="Last name as on license"
              required
            />
          </div>
        </div>

        {/* Date of Birth */}
        <div className="form-group">
          <label>Date of Birth *</label>
          <input
            type="date"
            value={formData.dateOfBirth}
            onChange={(e) => setFormData({
              ...formData,
              dateOfBirth: e.target.value
            })}
            required
          />
        </div>

        {/* License Details */}
        <div className="form-group">
          <label>Issued By *</label>
          <select
            value={formData.issuedBy}
            onChange={(e) => setFormData({
              ...formData,
              issuedBy: e.target.value
            })}
            required
          >
            <option value="">Select issuing authority</option>
            <option value="Department of Transport HCMC">Department of Transport - Ho Chi Minh City</option>
            <option value="Department of Transport Hanoi">Department of Transport - Hanoi</option>
            <option value="Department of Transport Da Nang">Department of Transport - Da Nang</option>
            <option value="Department of Transport Can Tho">Department of Transport - Can Tho</option>
            <option value="Other">Other (Please specify in notes)</option>
          </select>
        </div>

        <div className="form-row">
          <div className="form-group">
            <label>Issue Date *</label>
            <input
              type="date"
              value={formData.issueDate}
              onChange={(e) => setFormData({
                ...formData,
                issueDate: e.target.value
              })}
              required
            />
          </div>
          
          <div className="form-group">
            <label>Expiry Date (Optional)</label>
            <input
              type="date"
              value={formData.expiryDate}
              onChange={(e) => setFormData({
                ...formData,
                expiryDate: e.target.value
              })}
            />
          </div>
        </div>

        {/* License Image Upload */}
        <div className="form-group">
          <label>License Image *</label>
          <div className="image-upload-area">
            <input
              type="file"
              accept="image/*"
              onChange={handleImageChange}
              className="file-input"
              id="license-image"
              required
            />
            
            <label htmlFor="license-image" className="file-input-label">
              <div className="upload-icon">📷</div>
              <div className="upload-text">
                <p>Click to upload license image</p>
                <p className="upload-hint">PNG, JPG up to 5MB</p>
              </div>
            </label>
            
            {previewImage && (
              <div className="image-preview">
                <img src={previewImage} alt="License preview" />
                <button 
                  type="button" 
                  onClick={() => {
                    setPreviewImage('');
                    setFormData({ ...formData, licenseImage: null as any });
                  }}
                  className="remove-image"
                >
                  ✕
                </button>
              </div>
            )}
          </div>
          
          <div className="upload-guidelines">
            <h4>Image Guidelines:</h4>
            <ul>
              <li>Ensure the license is clearly visible and readable</li>
              <li>Avoid glare and shadows</li>
              <li>Include all four corners of the license</li>
              <li>Use good lighting for best results</li>
            </ul>
          </div>
        </div>

        {/* Submit Button */}
        <div className="form-actions">
          <button 
            type="submit" 
            disabled={isSubmitting || licenseExists === true}
            className="submit-btn"
          >
            {isSubmitting ? 'Submitting...' : 'Submit for Verification'}
          </button>
        </div>
      </form>
    </div>
  );
};
```

### 2. Check Verification Status

```typescript
interface LicenseStatus {
  verificationId: number;
  licenseNumber: string;
  status: 'pending' | 'verified' | 'rejected' | 'expired';
  submittedAt: string;
  verifiedAt?: string;
  verifiedBy?: string;
  rejectReason?: string;
  notes?: string;
  imageUrl?: string;
  expiryDate?: string;
  renewalRequired: boolean;
}

export const licenseService = {
  async getLicenseStatus(verificationId: number): Promise<BaseResponse<LicenseStatus>> {
    return await apiClient.get(`/shared/license/status/${verificationId}`);
  },

  async getMyLicenses(): Promise<BaseResponse<LicenseStatus[]>> {
    return await apiClient.get('/shared/license/my-licenses');
  }
};

// License status component
const LicenseStatus: React.FC<{ verificationId: number }> = ({ verificationId }) => {
  const [licenseStatus, setLicenseStatus] = useState<LicenseStatus | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadLicenseStatus();
    
    // Poll for status updates every 30 seconds if pending
    const interval = setInterval(() => {
      if (licenseStatus?.status === 'pending') {
        loadLicenseStatus();
      }
    }, 30000);

    return () => clearInterval(interval);
  }, [verificationId]);

  const loadLicenseStatus = async () => {
    try {
      const response = await licenseService.getLicenseStatus(verificationId);
      if (response.statusCode === 200) {
        setLicenseStatus(response.data);
      }
    } catch (error) {
      console.error('Failed to load license status:', error);
    } finally {
      setLoading(false);
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'pending': return '⏳';
      case 'verified': return '✅';
      case 'rejected': return '❌';
      case 'expired': return '⚠️';
      default: return '❓';
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'pending': return 'orange';
      case 'verified': return 'green';
      case 'rejected': return 'red';
      case 'expired': return 'yellow';
      default: return 'gray';
    }
  };

  if (loading) return <div className="loading">Loading license status...</div>;
  if (!licenseStatus) return <div className="error">License not found</div>;

  return (
    <div className="license-status">
      <div className="status-header">
        <h2>License Verification Status</h2>
        <div className={`status-badge ${getStatusColor(licenseStatus.status)}`}>
          <span className="status-icon">{getStatusIcon(licenseStatus.status)}</span>
          <span className="status-text">{licenseStatus.status.toUpperCase()}</span>
        </div>
      </div>

      <div className="license-details">
        <div className="detail-row">
          <span className="label">License Number:</span>
          <span className="value">{licenseStatus.licenseNumber}</span>
        </div>
        
        <div className="detail-row">
          <span className="label">Submitted:</span>
          <span className="value">{new Date(licenseStatus.submittedAt).toLocaleString()}</span>
        </div>

        {licenseStatus.verifiedAt && (
          <div className="detail-row">
            <span className="label">Verified:</span>
            <span className="value">{new Date(licenseStatus.verifiedAt).toLocaleString()}</span>
          </div>
        )}

        {licenseStatus.verifiedBy && (
          <div className="detail-row">
            <span className="label">Verified By:</span>
            <span className="value">{licenseStatus.verifiedBy}</span>
          </div>
        )}

        {licenseStatus.expiryDate && (
          <div className="detail-row">
            <span className="label">Expires:</span>
            <span className="value">{new Date(licenseStatus.expiryDate).toLocaleDateString()}</span>
          </div>
        )}
      </div>

      {/* Status-specific content */}
      {licenseStatus.status === 'pending' && (
        <div className="status-content pending">
          <h3>Verification in Progress</h3>
          <p>Your license is being reviewed by our verification team. This usually takes 1-3 business days.</p>
          <div className="progress-indicator">
            <div className="progress-step completed">
              <span>Submitted</span>
            </div>
            <div className="progress-step active">
              <span>Under Review</span>
            </div>
            <div className="progress-step">
              <span>Verified</span>
            </div>
          </div>
        </div>
      )}

      {licenseStatus.status === 'verified' && (
        <div className="status-content verified">
          <h3>🎉 License Verified Successfully!</h3>
          <p>Your driving license has been verified and you can now book vehicles.</p>
          
          {licenseStatus.renewalRequired && (
            <div className="renewal-notice">
              <h4>⚠️ Renewal Required</h4>
              <p>Your license will expire soon. Please renew it to continue using our services.</p>
            </div>
          )}
          
          <button 
            onClick={() => navigate('/book-vehicle')}
            className="action-btn primary"
          >
            Start Booking Vehicles
          </button>
        </div>
      )}

      {licenseStatus.status === 'rejected' && (
        <div className="status-content rejected">
          <h3>Verification Rejected</h3>
          <p>Unfortunately, your license verification was rejected for the following reason:</p>
          
          <div className="reject-reason">
            <h4>Reason:</h4>
            <p>{licenseStatus.rejectReason}</p>
          </div>
          
          {licenseStatus.notes && (
            <div className="additional-notes">
              <h4>Additional Notes:</h4>
              <p>{licenseStatus.notes}</p>
            </div>
          )}
          
          <div className="next-steps">
            <h4>Next Steps:</h4>
            <ul>
              <li>Review the rejection reason above</li>
              <li>Ensure your license information is correct</li>
              <li>Take a clearer photo of your license</li>
              <li>Submit a new verification request</li>
            </ul>
          </div>
          
          <button 
            onClick={() => navigate('/license/verify')}
            className="action-btn secondary"
          >
            Submit New Verification
          </button>
        </div>
      )}

      {licenseStatus.status === 'expired' && (
        <div className="status-content expired">
          <h3>License Expired</h3>
          <p>Your license has expired and needs to be renewed.</p>
          
          <div className="renewal-info">
            <h4>To continue using our services:</h4>
            <ol>
              <li>Renew your driving license with the appropriate authorities</li>
              <li>Submit a new verification request with your renewed license</li>
              <li>Wait for verification to complete</li>
            </ol>
          </div>
          
          <button 
            onClick={() => navigate('/license/verify')}
            className="action-btn primary"
          >
            Submit Renewed License
          </button>
        </div>
      )}

      {/* License Image */}
      {licenseStatus.imageUrl && (
        <div className="license-image-section">
          <h3>Submitted License Image</h3>
          <img 
            src={licenseStatus.imageUrl} 
            alt="License"
            className="license-image"
          />
        </div>
      )}
    </div>
  );
};
```

### 3. My Licenses Overview

```typescript
const MyLicenses: React.FC = () => {
  const [licenses, setLicenses] = useState<LicenseStatus[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadMyLicenses();
  }, []);

  const loadMyLicenses = async () => {
    try {
      const response = await licenseService.getMyLicenses();
      if (response.statusCode === 200) {
        setLicenses(response.data);
      }
    } catch (error) {
      console.error('Failed to load licenses:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="loading">Loading your licenses...</div>;

  return (
    <div className="my-licenses">
      <div className="page-header">
        <h2>My Driving Licenses</h2>
        <button 
          onClick={() => navigate('/license/verify')}
          className="add-license-btn"
        >
          Add New License
        </button>
      </div>

      {licenses.length === 0 ? (
        <div className="no-licenses">
          <div className="empty-state">
            <div className="empty-icon">🪪</div>
            <h3>No Licenses Found</h3>
            <p>You haven't submitted any driving licenses for verification yet.</p>
            <button 
              onClick={() => navigate('/license/verify')}
              className="action-btn primary"
            >
              Verify Your License
            </button>
          </div>
        </div>
      ) : (
        <div className="licenses-grid">
          {licenses.map(license => (
            <div key={license.verificationId} className="license-card">
              <div className="license-header">
                <h3>{license.licenseNumber}</h3>
                <span className={`status-badge ${getStatusColor(license.status)}`}>
                  {license.status}
                </span>
              </div>
              
              <div className="license-info">
                <p>Submitted: {new Date(license.submittedAt).toLocaleDateString()}</p>
                
                {license.verifiedAt && (
                  <p>Verified: {new Date(license.verifiedAt).toLocaleDateString()}</p>
                )}
                
                {license.expiryDate && (
                  <p>Expires: {new Date(license.expiryDate).toLocaleDateString()}</p>
                )}
              </div>
              
              <div className="license-actions">
                <button 
                  onClick={() => navigate(`/license/status/${license.verificationId}`)}
                  className="view-details-btn"
                >
                  View Details
                </button>
                
                {license.status === 'rejected' && (
                  <button 
                    onClick={() => navigate('/license/verify')}
                    className="retry-btn"
                  >
                    Try Again
                  </button>
                )}
                
                {license.renewalRequired && (
                  <button 
                    onClick={() => navigate('/license/verify')}
                    className="renew-btn"
                  >
                    Renew License
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
```

## 🎨 UI Components & Styling

### Form Validation
```typescript
// utils/licenseValidation.ts
export const validateLicenseForm = (data: VerifyLicenseRequest): string[] => {
  const errors: string[] = [];
  
  if (!data.licenseNumber || data.licenseNumber.length < 8) {
    errors.push('License number must be at least 8 characters');
  }
  
  if (!data.firstName || data.firstName.length < 2) {
    errors.push('First name must be at least 2 characters');
  }
  
  if (!data.lastName || data.lastName.length < 2) {
    errors.push('Last name must be at least 2 characters');
  }
  
  const birthDate = new Date(data.dateOfBirth);
  const today = new Date();
  const age = today.getFullYear() - birthDate.getFullYear();
  
  if (age < 18) {
    errors.push('You must be at least 18 years old');
  }
  
  if (age > 100) {
    errors.push('Please check your date of birth');
  }
  
  const issueDate = new Date(data.issueDate);
  if (issueDate > today) {
    errors.push('Issue date cannot be in the future');
  }
  
  if (data.expiryDate) {
    const expiryDate = new Date(data.expiryDate);
    if (expiryDate <= issueDate) {
      errors.push('Expiry date must be after issue date');
    }
  }
  
  return errors;
};
```

### CSS Classes
```css
/* License-specific styles */
.license-verification-form {
  max-width: 600px;
  margin: 0 auto;
  padding: 2rem;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border-radius: 20px;
  font-weight: 600;
  font-size: 0.875rem;
}

.status-badge.pending { background: #fef3cd; color: #856404; }
.status-badge.verified { background: #d1edff; color: #0f5132; }
.status-badge.rejected { background: #f8d7da; color: #721c24; }
.status-badge.expired { background: #fff3cd; color: #856404; }

.image-upload-area {
  border: 2px dashed #ddd;
  border-radius: 8px;
  padding: 2rem;
  text-align: center;
  transition: border-color 0.3s;
}

.image-upload-area:hover {
  border-color: #007bff;
}

.image-preview {
  position: relative;
  max-width: 300px;
  margin: 1rem auto;
}

.image-preview img {
  width: 100%;
  border-radius: 8px;
}

.remove-image {
  position: absolute;
  top: -10px;
  right: -10px;
  background: #dc3545;
  color: white;
  border: none;
  border-radius: 50%;
  width: 30px;
  height: 30px;
  cursor: pointer;
}

.progress-indicator {
  display: flex;
  justify-content: space-between;
  margin: 2rem 0;
}

.progress-step {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex: 1;
  position: relative;
}

.progress-step.completed::before {
  content: '✓';
  background: #28a745;
  color: white;
}

.progress-step.active::before {
  content: '⏳';
  background: #ffc107;
  color: #212529;
}

.progress-step::before {
  content: '';
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #6c757d;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 0.5rem;
}
```

## 🛡️ Error Handling

```typescript
// utils/licenseErrorHandler.ts
export const handleLicenseError = (error: any) => {
  const statusCode = error.response?.status;
  const message = error.response?.data?.message;
  
  switch (statusCode) {
    case 400:
      return getLicenseValidationError(message);
    case 409:
      return 'This license number is already registered in our system';
    case 413:
      return 'Image file is too large. Please choose a smaller file (max 5MB)';
    case 415:
      return 'Invalid file format. Please upload a JPG, PNG, or GIF image';
    case 500:
      return 'Server error occurred. Please try again later';
    default:
      return 'An unexpected error occurred during license verification';
  }
};

const getLicenseValidationError = (message: string) => {
  const errorMap: { [key: string]: string } = {
    'LICENSE_NUMBER_REQUIRED': 'License number is required',
    'LICENSE_NUMBER_INVALID_FORMAT': 'Invalid license number format',
    'LICENSE_ALREADY_REGISTERED': 'This license is already registered',
    'FIRST_NAME_REQUIRED': 'First name is required',
    'LAST_NAME_REQUIRED': 'Last name is required',
    'DATE_OF_BIRTH_REQUIRED': 'Date of birth is required',
    'MUST_BE_AT_LEAST_18_YEARS_OLD': 'You must be at least 18 years old',
    'ISSUE_DATE_REQUIRED': 'License issue date is required',
    'ISSUE_DATE_CANNOT_BE_FUTURE': 'Issue date cannot be in the future',
    'ISSUED_BY_REQUIRED': 'Issuing authority is required',
    'INVALID_IMAGE_FILE': 'Please upload a valid image file',
    'IMAGE_SIZE_TOO_LARGE': 'Image size must be less than 5MB'
  };
  
  return errorMap[message] || message || 'Validation error occurred';
};
```

---

**Next Step**: Tôi sẽ tiếp tục tạo file `README_FRONTEND_GROUP.md` trong message tiếp theo.