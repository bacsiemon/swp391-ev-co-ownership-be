# File Upload Integration Guide

## 📁 Overview

This guide covers file upload, download, and management functionality including images, documents, and various file types for the EV Co-Ownership platform.

## 🏗️ File Upload API Structure

File endpoints support comprehensive file management:
- **Base URL**: `/api/FileUpload`
- **Authentication**: Required for all endpoints
- **File Size Limit**: 10MB maximum
- **Security**: File type validation and malware scanning

## 📋 Supported File Types

### Image Files
- **JPEG, JPG**: Standard photo format
- **PNG**: High-quality images with transparency
- **GIF**: Animated images
- **WEBP**: Modern web-optimized format

### Document Files  
- **PDF**: Portable documents
- **DOC, DOCX**: Microsoft Word documents
- **XLS, XLSX**: Microsoft Excel spreadsheets
- **TXT**: Plain text files

## 🚀 File Upload Implementation

### 1. Basic File Upload

```typescript
interface FileUploadResponse {
  id: number;
  fileName: string;
  fileSize: number;
  mimeType: string;
  uploadDate: string;
  downloadUrl: string;
  fileUrl: string;
}

interface FileUploadRequest {
  file: File;
  category?: 'profile' | 'license' | 'vehicle' | 'maintenance' | 'document';
  description?: string;
}

export const fileUploadService = {
  async uploadFile(file: File, category?: string): Promise<BaseResponse<FileUploadResponse>> {
    const formData = new FormData();
    formData.append('file', file);
    
    if (category) {
      formData.append('category', category);
    }

    return await apiClient.post('/FileUpload/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        const percentCompleted = Math.round(
          (progressEvent.loaded * 100) / (progressEvent.total || 1)
        );
        // Handle upload progress
        console.log(`Upload progress: ${percentCompleted}%`);
      },
    });
  },

  async getFileInfo(fileId: number): Promise<BaseResponse<FileUploadResponse>> {
    return await apiClient.get(`/FileUpload/${fileId}/info`);
  },

  async downloadFile(fileId: number): Promise<Blob> {
    const response = await apiClient.get(`/FileUpload/${fileId}/download`, {
      responseType: 'blob',
    });
    return response.data;
  },

  async deleteFile(fileId: number): Promise<BaseResponse<void>> {
    return await apiClient.delete(`/FileUpload/${fileId}`);
  },

  getFileUrl(fileId: number): string {
    return `${API_BASE_URL}/FileUpload/${fileId}`;
  }
};

// File upload component
const FileUpload: React.FC<{
  onUploadSuccess: (file: FileUploadResponse) => void;
  onUploadError: (error: string) => void;
  acceptedTypes?: string[];
  maxSize?: number;
  category?: string;
  multiple?: boolean;
}> = ({ 
  onUploadSuccess, 
  onUploadError, 
  acceptedTypes = ['image/*', '.pdf', '.doc', '.docx'], 
  maxSize = 10 * 1024 * 1024, // 10MB
  category,
  multiple = false 
}) => {
  const [uploading, setUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [dragActive, setDragActive] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const validateFile = (file: File): string | null => {
    // Check file size
    if (file.size > maxSize) {
      return `File size exceeds limit. Maximum size is ${formatFileSize(maxSize)}.`;
    }

    // Check file type
    const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase();
    const mimeType = file.type.toLowerCase();
    
    const isValidType = acceptedTypes.some(type => {
      if (type.startsWith('.')) {
        return fileExtension === type;
      } else if (type.includes('/*')) {
        return mimeType.startsWith(type.replace('/*', '/'));
      } else {
        return mimeType === type;
      }
    });

    if (!isValidType) {
      return `Invalid file type. Accepted types: ${acceptedTypes.join(', ')}`;
    }

    return null;
  };

  const handleFileUpload = async (files: FileList) => {
    const file = files[0]; // For single file upload
    
    const validationError = validateFile(file);
    if (validationError) {
      onUploadError(validationError);
      return;
    }

    setUploading(true);
    setUploadProgress(0);

    try {
      // Create custom axios instance for progress tracking
      const response = await apiClient.post('/FileUpload/upload', (() => {
        const formData = new FormData();
        formData.append('file', file);
        if (category) formData.append('category', category);
        return formData;
      })(), {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: (progressEvent) => {
          const percentCompleted = Math.round(
            (progressEvent.loaded * 100) / (progressEvent.total || 1)
          );
          setUploadProgress(percentCompleted);
        },
      });

      if (response.data.statusCode === 201) {
        onUploadSuccess(response.data.data);
        toast.success('File uploaded successfully!');
      } else {
        throw new Error(response.data.message || 'Upload failed');
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || error.message || 'Upload failed';
      onUploadError(errorMessage);
      toast.error(errorMessage);
    } finally {
      setUploading(false);
      setUploadProgress(0);
    }
  };

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      handleFileUpload(e.dataTransfer.files);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      handleFileUpload(e.target.files);
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <div className="file-upload">
      <div 
        className={`upload-area ${dragActive ? 'drag-active' : ''} ${uploading ? 'uploading' : ''}`}
        onDragEnter={handleDrag}
        onDragLeave={handleDrag}
        onDragOver={handleDrag}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
      >
        <input
          ref={fileInputRef}
          type="file"
          onChange={handleInputChange}
          accept={acceptedTypes.join(',')}
          multiple={multiple}
          style={{ display: 'none' }}
        />

        {uploading ? (
          <div className="upload-progress">
            <div className="upload-icon">📤</div>
            <div className="progress-info">
              <p>Uploading... {uploadProgress}%</p>
              <div className="progress-bar">
                <div 
                  className="progress-fill" 
                  style={{ width: `${uploadProgress}%` }}
                />
              </div>
            </div>
          </div>
        ) : (
          <div className="upload-prompt">
            <div className="upload-icon">📁</div>
            <div className="upload-text">
              <p className="primary-text">
                {dragActive ? 'Drop files here' : 'Click to upload or drag and drop'}
              </p>
              <p className="secondary-text">
                Max file size: {formatFileSize(maxSize)}
              </p>
              <p className="supported-types">
                Supported: {acceptedTypes.join(', ')}
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
```

### 2. Image Upload with Preview

```typescript
const ImageUpload: React.FC<{
  onImageUpload: (file: FileUploadResponse) => void;
  currentImageUrl?: string;
  category: string;
  aspectRatio?: number;
}> = ({ onImageUpload, currentImageUrl, category, aspectRatio = 1 }) => {
  const [preview, setPreview] = useState<string>(currentImageUrl || '');
  const [uploading, setUploading] = useState(false);

  const handleImageSelect = async (files: FileList) => {
    const file = files[0];
    
    // Validate file is an image
    if (!file.type.startsWith('image/')) {
      toast.error('Please select an image file');
      return;
    }

    // Create preview
    const reader = new FileReader();
    reader.onload = (e) => {
      setPreview(e.target?.result as string);
    };
    reader.readAsDataURL(file);

    // Upload file
    setUploading(true);
    try {
      const response = await fileUploadService.uploadFile(file, category);
      if (response.statusCode === 201) {
        onImageUpload(response.data);
        toast.success('Image uploaded successfully!');
      }
    } catch (error: any) {
      toast.error(error.response?.data?.message || 'Upload failed');
      setPreview(currentImageUrl || '');
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="image-upload">
      <div className="image-preview-container">
        {preview ? (
          <div className="image-preview" style={{ aspectRatio }}>
            <img src={preview} alt="Preview" />
            <div className="image-overlay">
              <button 
                onClick={() => document.getElementById('image-input')?.click()}
                className="change-image-btn"
                disabled={uploading}
              >
                {uploading ? 'Uploading...' : 'Change Image'}
              </button>
            </div>
          </div>
        ) : (
          <div 
            className="image-placeholder" 
            style={{ aspectRatio }}
            onClick={() => document.getElementById('image-input')?.click()}
          >
            <div className="placeholder-content">
              <div className="placeholder-icon">📷</div>
              <p>Click to upload image</p>
            </div>
          </div>
        )}
      </div>

      <input
        id="image-input"
        type="file"
        accept="image/*"
        onChange={(e) => e.target.files && handleImageSelect(e.target.files)}
        style={{ display: 'none' }}
      />

      <div className="upload-guidelines">
        <h4>Image Guidelines:</h4>
        <ul>
          <li>Maximum file size: 10MB</li>
          <li>Supported formats: JPG, PNG, GIF, WEBP</li>
          <li>Recommended aspect ratio: {aspectRatio}:1</li>
          <li>Minimum resolution: 400x400px</li>
        </ul>
      </div>
    </div>
  );
};
```

### 3. Document Upload Component

```typescript
interface DocumentUploadProps {
  onDocumentUpload: (file: FileUploadResponse) => void;
  category: string;
  allowedTypes?: string[];
  maxSize?: number;
  title?: string;
  description?: string;
}

const DocumentUpload: React.FC<DocumentUploadProps> = ({
  onDocumentUpload,
  category,
  allowedTypes = ['.pdf', '.doc', '.docx', '.xls', '.xlsx', '.txt'],
  maxSize = 10 * 1024 * 1024,
  title = 'Upload Document',
  description = 'Upload supporting documents'
}) => {
  const [uploadedFiles, setUploadedFiles] = useState<FileUploadResponse[]>([]);
  const [uploading, setUploading] = useState(false);

  const handleDocumentUpload = async (files: FileList) => {
    const file = files[0];
    
    setUploading(true);
    try {
      const response = await fileUploadService.uploadFile(file, category);
      if (response.statusCode === 201) {
        const newFile = response.data;
        setUploadedFiles(prev => [...prev, newFile]);
        onDocumentUpload(newFile);
        toast.success('Document uploaded successfully!');
      }
    } catch (error: any) {
      toast.error(error.response?.data?.message || 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  const handleDownload = async (file: FileUploadResponse) => {
    try {
      const blob = await fileUploadService.downloadFile(file.id);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = file.fileName;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      toast.error('Failed to download file');
    }
  };

  const handleRemove = async (fileId: number) => {
    if (!confirm('Are you sure you want to remove this file?')) return;

    try {
      await fileUploadService.deleteFile(fileId);
      setUploadedFiles(prev => prev.filter(f => f.id !== fileId));
      toast.success('File removed successfully');
    } catch (error: any) {
      toast.error(error.response?.data?.message || 'Failed to remove file');
    }
  };

  const getFileIcon = (mimeType: string) => {
    if (mimeType.includes('pdf')) return '📄';
    if (mimeType.includes('word') || mimeType.includes('document')) return '📝';
    if (mimeType.includes('spreadsheet') || mimeType.includes('excel')) return '📊';
    if (mimeType.includes('text')) return '📃';
    return '📎';
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <div className="document-upload">
      <div className="upload-header">
        <h3>{title}</h3>
        <p>{description}</p>
      </div>

      <FileUpload
        onUploadSuccess={onDocumentUpload}
        onUploadError={(error) => toast.error(error)}
        acceptedTypes={allowedTypes}
        maxSize={maxSize}
        category={category}
      />

      {uploadedFiles.length > 0 && (
        <div className="uploaded-files">
          <h4>Uploaded Documents</h4>
          <div className="files-list">
            {uploadedFiles.map(file => (
              <div key={file.id} className="file-item">
                <div className="file-info">
                  <span className="file-icon">
                    {getFileIcon(file.mimeType)}
                  </span>
                  <div className="file-details">
                    <span className="file-name">{file.fileName}</span>
                    <span className="file-meta">
                      {formatFileSize(file.fileSize)} • {new Date(file.uploadDate).toLocaleDateString()}
                    </span>
                  </div>
                </div>
                
                <div className="file-actions">
                  <button 
                    onClick={() => handleDownload(file)}
                    className="download-btn"
                    title="Download"
                  >
                    ⬇️
                  </button>
                  <button 
                    onClick={() => handleRemove(file.id)}
                    className="remove-btn"
                    title="Remove"
                  >
                    🗑️
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
```

## 🖼️ Profile Picture Management

```typescript
const ProfilePictureUpload: React.FC<{
  currentImageUrl?: string;
  onImageUpdate: (imageUrl: string) => void;
  userId: number;
}> = ({ currentImageUrl, onImageUpdate, userId }) => {
  const [uploading, setUploading] = useState(false);
  const [preview, setPreview] = useState(currentImageUrl);

  const handleProfilePictureUpload = async (files: FileList) => {
    const file = files[0];
    
    // Validate image file
    if (!file.type.startsWith('image/')) {
      toast.error('Please select an image file');
      return;
    }

    // Check file size (max 2MB for profile pictures)
    if (file.size > 2 * 1024 * 1024) {
      toast.error('Image size must be less than 2MB');
      return;
    }

    setUploading(true);
    try {
      const response = await fileUploadService.uploadFile(file, 'profile');
      if (response.statusCode === 201) {
        const imageUrl = fileUploadService.getFileUrl(response.data.id);
        setPreview(imageUrl);
        onImageUpdate(imageUrl);
        toast.success('Profile picture updated successfully!');
      }
    } catch (error: any) {
      toast.error(error.response?.data?.message || 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="profile-picture-upload">
      <div className="profile-picture-container">
        <div className="profile-picture">
          {preview ? (
            <img src={preview} alt="Profile" />
          ) : (
            <div className="default-avatar">
              <span className="avatar-icon">👤</span>
            </div>
          )}
          
          <div className="upload-overlay">
            <input
              type="file"
              accept="image/*"
              onChange={(e) => e.target.files && handleProfilePictureUpload(e.target.files)}
              style={{ display: 'none' }}
              id="profile-picture-input"
            />
            <label 
              htmlFor="profile-picture-input" 
              className="upload-btn"
            >
              {uploading ? '⏳' : '📷'}
            </label>
          </div>
        </div>
      </div>

      <div className="upload-info">
        <p>Click the camera icon to change your profile picture</p>
        <small>JPG, PNG up to 2MB • Square images work best</small>
      </div>
    </div>
  );
};
```

## 🚗 Vehicle Image Gallery

```typescript
interface VehicleImageGalleryProps {
  vehicleId: number;
  images: FileUploadResponse[];
  onImagesUpdate: (images: FileUploadResponse[]) => void;
  maxImages?: number;
}

const VehicleImageGallery: React.FC<VehicleImageGalleryProps> = ({
  vehicleId,
  images,
  onImagesUpdate,
  maxImages = 10
}) => {
  const [selectedImage, setSelectedImage] = useState<FileUploadResponse | null>(null);
  const [uploading, setUploading] = useState(false);

  const handleImageUpload = async (files: FileList) => {
    if (images.length >= maxImages) {
      toast.error(`Maximum ${maxImages} images allowed`);
      return;
    }

    const file = files[0];
    
    setUploading(true);
    try {
      const response = await fileUploadService.uploadFile(file, 'vehicle');
      if (response.statusCode === 201) {
        const newImages = [...images, response.data];
        onImagesUpdate(newImages);
        toast.success('Vehicle image uploaded successfully!');
      }
    } catch (error: any) {
      toast.error(error.response?.data?.message || 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  const handleRemoveImage = async (imageId: number) => {
    if (!confirm('Are you sure you want to remove this image?')) return;

    try {
      await fileUploadService.deleteFile(imageId);
      const updatedImages = images.filter(img => img.id !== imageId);
      onImagesUpdate(updatedImages);
      toast.success('Image removed successfully');
    } catch (error: any) {
      toast.error('Failed to remove image');
    }
  };

  return (
    <div className="vehicle-image-gallery">
      <div className="gallery-header">
        <h3>Vehicle Images ({images.length}/{maxImages})</h3>
      </div>

      <div className="image-grid">
        {images.map(image => (
          <div key={image.id} className="image-item">
            <img 
              src={fileUploadService.getFileUrl(image.id)}
              alt={image.fileName}
              onClick={() => setSelectedImage(image)}
            />
            <div className="image-overlay">
              <button 
                onClick={() => handleRemoveImage(image.id)}
                className="remove-image-btn"
              >
                🗑️
              </button>
            </div>
          </div>
        ))}

        {images.length < maxImages && (
          <div className="add-image-slot">
            <input
              type="file"
              accept="image/*"
              onChange={(e) => e.target.files && handleImageUpload(e.target.files)}
              style={{ display: 'none' }}
              id="vehicle-image-input"
            />
            <label 
              htmlFor="vehicle-image-input"
              className={`add-image-btn ${uploading ? 'uploading' : ''}`}
            >
              {uploading ? '⏳' : '➕'}
              <span>Add Image</span>
            </label>
          </div>
        )}
      </div>

      {selectedImage && (
        <ImageModal
          image={selectedImage}
          onClose={() => setSelectedImage(null)}
          onDelete={handleRemoveImage}
        />
      )}
    </div>
  );
};

// Image modal for full-size viewing
const ImageModal: React.FC<{
  image: FileUploadResponse;
  onClose: () => void;
  onDelete: (id: number) => void;
}> = ({ image, onClose, onDelete }) => {
  return (
    <div className="image-modal-overlay" onClick={onClose}>
      <div className="image-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h3>{image.fileName}</h3>
          <div className="modal-actions">
            <button onClick={() => onDelete(image.id)} className="delete-btn">
              Delete
            </button>
            <button onClick={onClose} className="close-btn">
              ✕
            </button>
          </div>
        </div>
        
        <div className="modal-image">
          <img src={fileUploadService.getFileUrl(image.id)} alt={image.fileName} />
        </div>
        
        <div className="modal-info">
          <p>Size: {formatFileSize(image.fileSize)}</p>
          <p>Uploaded: {new Date(image.uploadDate).toLocaleString()}</p>
        </div>
      </div>
    </div>
  );
};
```

## 🔧 File Validation & Security

```typescript
// utils/fileValidation.ts
export const FileValidation = {
  // File type validation
  validateFileType: (file: File, allowedTypes: string[]): boolean => {
    const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase();
    const mimeType = file.type.toLowerCase();
    
    return allowedTypes.some(type => {
      if (type.startsWith('.')) {
        return fileExtension === type;
      } else if (type.includes('/*')) {
        return mimeType.startsWith(type.replace('/*', '/'));
      } else {
        return mimeType === type;
      }
    });
  },

  // File size validation
  validateFileSize: (file: File, maxSize: number): boolean => {
    return file.size <= maxSize;
  },

  // Image dimension validation
  validateImageDimensions: (file: File, minWidth: number, minHeight: number): Promise<boolean> => {
    return new Promise((resolve) => {
      const img = new Image();
      img.onload = () => {
        resolve(img.width >= minWidth && img.height >= minHeight);
      };
      img.onerror = () => resolve(false);
      img.src = URL.createObjectURL(file);
    });
  },

  // Generate secure filename
  generateSecureFilename: (originalName: string): string => {
    const extension = originalName.split('.').pop();
    const timestamp = Date.now();
    const randomString = Math.random().toString(36).substring(2);
    return `${timestamp}_${randomString}.${extension}`;
  },

  // File type detection by content (MIME type verification)
  detectFileType: (file: File): Promise<string> => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (e) => {
        const uint = new Uint8Array(e.target?.result as ArrayBuffer);
        const bytes: number[] = [];
        uint.forEach((byte) => {
          bytes.push(byte);
        });
        
        const hex = bytes.map(byte => byte.toString(16).padStart(2, '0')).join('');
        const detectedType = getFileTypeFromSignature(hex);
        resolve(detectedType);
      };
      reader.onerror = reject;
      reader.readAsArrayBuffer(file.slice(0, 4));
    });
  }
};

// File type signatures for security validation
const getFileTypeFromSignature = (hex: string): string => {
  const signatures: { [key: string]: string } = {
    '89504e47': 'image/png',
    'ffd8ffe0': 'image/jpeg',
    'ffd8ffe1': 'image/jpeg',
    'ffd8ffe2': 'image/jpeg',
    '47494638': 'image/gif',
    '52494646': 'image/webp',
    '25504446': 'application/pdf',
    '504b0304': 'application/zip', // Also used by .docx, .xlsx
    'd0cf11e0': 'application/msword', // .doc, .xls
  };
  
  const signature = hex.substring(0, 8);
  return signatures[signature] || 'application/octet-stream';
};

// Error handling for file operations
export const handleFileError = (error: any): string => {
  const statusCode = error.response?.status;
  const message = error.response?.data?.message;
  
  switch (statusCode) {
    case 400:
      return getFileValidationError(message);
    case 413:
      return 'File size exceeds the maximum limit';
    case 415:
      return 'File type not supported';
    case 500:
      return 'Server error occurred during file processing';
    default:
      return 'An unexpected error occurred during file operation';
  }
};

const getFileValidationError = (message: string): string => {
  const errorMap: { [key: string]: string } = {
    'FILE_REQUIRED': 'Please select a file to upload',
    'INVALID_FILE_TYPE': 'File type not supported',
    'FILE_SIZE_EXCEEDS_LIMIT': 'File size exceeds the maximum limit',
    'FILE_NOT_FOUND': 'File not found',
    'FILE_UPLOAD_FAILED': 'File upload failed',
    'FILE_DELETE_FAILED': 'Failed to delete file',
    'MALWARE_DETECTED': 'File failed security scan',
    'INVALID_FILE_CONTENT': 'File content is not valid'
  };
  
  return errorMap[message] || message || 'File operation failed';
};
```

## 🎨 CSS Styling

```css
/* File Upload Styles */
.file-upload {
  width: 100%;
}

.upload-area {
  border: 2px dashed #d0d7de;
  border-radius: 12px;
  padding: 2rem;
  text-align: center;
  cursor: pointer;
  transition: all 0.3s ease;
  background: #f8f9fa;
}

.upload-area:hover {
  border-color: #0969da;
  background: #f0f6ff;
}

.upload-area.drag-active {
  border-color: #0969da;
  background: #dbeafe;
  transform: scale(1.02);
}

.upload-area.uploading {
  cursor: not-allowed;
  opacity: 0.7;
}

.upload-progress {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.progress-bar {
  width: 100%;
  height: 8px;
  background: #e5e7eb;
  border-radius: 4px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #0969da, #2ea043);
  transition: width 0.3s ease;
}

/* Image Upload Styles */
.image-upload {
  width: 100%;
}

.image-preview-container {
  position: relative;
  display: inline-block;
}

.image-preview {
  position: relative;
  width: 200px;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}

.image-preview img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.image-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: opacity 0.3s ease;
}

.image-preview:hover .image-overlay {
  opacity: 1;
}

.change-image-btn {
  background: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
}

.image-placeholder {
  width: 200px;
  border: 2px dashed #d0d7de;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.3s ease;
  background: #f8f9fa;
}

.image-placeholder:hover {
  border-color: #0969da;
  background: #f0f6ff;
}

/* Document Upload Styles */
.document-upload {
  width: 100%;
}

.uploaded-files {
  margin-top: 2rem;
}

.files-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.file-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  background: white;
}

.file-info {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.file-icon {
  font-size: 1.5rem;
}

.file-details {
  display: flex;
  flex-direction: column;
}

.file-name {
  font-weight: 500;
  color: #1f2937;
}

.file-meta {
  font-size: 0.875rem;
  color: #6b7280;
}

.file-actions {
  display: flex;
  gap: 0.5rem;
}

.file-actions button {
  background: none;
  border: none;
  padding: 0.5rem;
  border-radius: 4px;
  cursor: pointer;
  transition: background 0.2s ease;
}

.file-actions button:hover {
  background: #f3f4f6;
}

/* Vehicle Image Gallery */
.vehicle-image-gallery {
  width: 100%;
}

.image-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: 1rem;
  margin-top: 1rem;
}

.image-item {
  position: relative;
  aspect-ratio: 1;
  border-radius: 8px;
  overflow: hidden;
  cursor: pointer;
}

.image-item img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s ease;
}

.image-item:hover img {
  transform: scale(1.05);
}

.image-overlay {
  position: absolute;
  top: 0;
  right: 0;
  padding: 0.5rem;
  opacity: 0;
  transition: opacity 0.3s ease;
}

.image-item:hover .image-overlay {
  opacity: 1;
}

.add-image-slot {
  aspect-ratio: 1;
  border: 2px dashed #d0d7de;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.add-image-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  padding: 1rem;
  color: #6b7280;
  transition: color 0.3s ease;
}

.add-image-btn:hover {
  color: #0969da;
}

/* Profile Picture */
.profile-picture-upload {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.profile-picture-container {
  position: relative;
}

.profile-picture {
  width: 120px;
  height: 120px;
  border-radius: 50%;
  overflow: hidden;
  position: relative;
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}

.profile-picture img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.default-avatar {
  width: 100%;
  height: 100%;
  background: #f3f4f6;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 2rem;
}

.upload-overlay {
  position: absolute;
  bottom: 0;
  right: 0;
  background: #0969da;
  border-radius: 50%;
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  box-shadow: 0 2px 8px rgba(0,0,0,0.15);
}

.upload-btn {
  color: white;
  font-size: 1rem;
  cursor: pointer;
}

/* Image Modal */
.image-modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.image-modal {
  background: white;
  border-radius: 12px;
  max-width: 90vw;
  max-height: 90vh;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  border-bottom: 1px solid #e5e7eb;
}

.modal-image {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

.modal-image img {
  max-width: 100%;
  max-height: 70vh;
  object-fit: contain;
}

.modal-info {
  padding: 1rem;
  border-top: 1px solid #e5e7eb;
  background: #f8f9fa;
}
```

---

Perfect! Tôi đã hoàn thành file `README_FRONTEND_FILEUPLOAD.md` với đầy đủ hệ thống quản lý file upload. 

## 🎉 **Tổng kết bộ README Files hoàn chỉnh:**

1. ✅ **README_FRONTEND_MAIN.md** - Hướng dẫn tổng quan và cấu hình
2. ✅ **README_FRONTEND_AUTH.md** - Hệ thống xác thực và ủy quyền  
3. ✅ **README_FRONTEND_ADMIN.md** - Tính năng quản trị viên
4. ✅ **README_FRONTEND_STAFF.md** - Tính năng nhân viên
5. ✅ **README_FRONTEND_COOWNER.md** - Tính năng đồng sở hữu
6. ✅ **README_FRONTEND_LICENSE.md** - Xác minh bằng lái xe
7. ✅ **README_FRONTEND_GROUP.md** - Quản lý nhóm và xe
8. ✅ **README_FRONTEND_FILEUPLOAD.md** - Hệ thống upload file

## 🚀 **File Upload README bao gồm:**

- **Basic file upload** với drag & drop
- **Image upload** với preview và cropping
- **Document management** với download/delete
- **Profile picture** management
- **Vehicle image gallery** với modal view
- **File validation & security** với type checking
- **Progress tracking** và error handling
- **Responsive CSS styling** cho tất cả components

Bộ README files này cung cấp hướng dẫn đầy đủ cho frontend React team để tích hợp với backend API một cách hiệu quả! 🎯