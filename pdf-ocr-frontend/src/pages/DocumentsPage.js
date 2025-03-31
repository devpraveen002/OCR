import React, { useState, useEffect } from 'react';
import { 
  Card, 
  Table, 
  Form, 
  InputGroup, 
  Button, 
  Badge, 
  Spinner, 
  Alert 
} from 'react-bootstrap';
import { Link } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faSearch, 
  faFileAlt, 
  faCheckCircle, 
  faExclamationTriangle, 
  faEye, 
  faTrash 
} from '@fortawesome/free-solid-svg-icons';
import axios from 'axios';
import { API_URL } from '../config';

const DocumentsPage = () => {
  const [documents, setDocuments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [deleteConfirm, setDeleteConfirm] = useState(null);

  useEffect(() => {
    fetchDocuments();
  }, []);

  const fetchDocuments = async (query = '') => {
    setLoading(true);
    try {
      const url = query
        ? `${API_URL}/api/documents/search?query=${encodeURIComponent(query)}`
        : `${API_URL}/api/documents`;
      
      const response = await axios.get(url);
      setDocuments(response.data);
      setError('');
    } catch (err) {
      setError('Failed to fetch documents');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e) => {
    e.preventDefault();
    fetchDocuments(searchQuery);
  };

  const handleDelete = async (id) => {
    try {
      await axios.delete(`${API_URL}/api/documents/${id}`);
      setDocuments(documents.filter(doc => doc.id !== id));
      setDeleteConfirm(null);
    } catch (err) {
      setError('Failed to delete document');
      console.error(err);
    }
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  };

  return (
    <div className="documents-page">
      <Card className="shadow-sm">
        <Card.Header as="h5">Document Library</Card.Header>
        <Card.Body>
          {error && <Alert variant="danger">{error}</Alert>}
          
          <Form onSubmit={handleSearch} className="mb-4">
            <InputGroup>
              <Form.Control
                placeholder="Search by filename or document type..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
              <Button variant="outline-secondary" type="submit">
                <FontAwesomeIcon icon={faSearch} />
              </Button>
            </InputGroup>
          </Form>
          
          {loading ? (
            <div className="text-center py-5">
              <Spinner animation="border" role="status" variant="primary">
                <span className="visually-hidden">Loading...</span>
              </Spinner>
              <p className="mt-2">Loading documents...</p>
            </div>
          ) : documents.length === 0 ? (
            <Alert variant="info">
              No documents found. Upload a PDF to get started.
              <div className="mt-2">
                <Link to="/" className="btn btn-primary btn-sm">
                  Upload Document
                </Link>
              </div>
            </Alert>
          ) : (
            <div className="table-responsive">
              <Table hover>
                <thead>
                  <tr>
                    <th>Filename</th>
                    <th>Type</th>
                    <th>Upload Date</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {documents.map((doc) => (
                    <tr key={doc.id}>
                      <td>
                        <FontAwesomeIcon icon={faFileAlt} className="me-2 text-primary" />
                        {doc.fileName}
                      </td>
                      <td>
                        <Badge bg="info">{doc.documentType || 'Unknown'}</Badge>
                      </td>
                      <td>{formatDate(doc.uploadDate)}</td>
                      <td>
                        {doc.isProcessed ? (
                          <Badge bg="success">
                            <FontAwesomeIcon icon={faCheckCircle} className="me-1" />
                            Processed
                          </Badge>
                        ) : (
                          <Badge bg="warning" text="dark">
                            <FontAwesomeIcon icon={faExclamationTriangle} className="me-1" />
                            Failed
                          </Badge>
                        )}
                      </td>
                      <td>
                        <Link 
                          to={`/documents/${doc.id}`} 
                          className="btn btn-sm btn-outline-primary me-1"
                          title="View details"
                        >
                          <FontAwesomeIcon icon={faEye} />
                        </Link>
                        
                        {deleteConfirm === doc.id ? (
                          <>
                            <Button 
                              variant="danger" 
                              size="sm" 
                              className="me-1"
                              onClick={() => handleDelete(doc.id)}
                            >
                              Confirm
                            </Button>
                            <Button 
                              variant="secondary" 
                              size="sm"
                              onClick={() => setDeleteConfirm(null)}
                            >
                              Cancel
                            </Button>
                          </>
                        ) : (
                          <Button 
                            variant="outline-danger" 
                            size="sm"
                            title="Delete document"
                            onClick={() => setDeleteConfirm(doc.id)}
                          >
                            <FontAwesomeIcon icon={faTrash} />
                          </Button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </div>
          )}
        </Card.Body>
      </Card>
    </div>
  );
};

export default DocumentsPage;